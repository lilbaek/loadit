using System;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Interprocess.Models;
using MessagePack;

namespace Loadit.Interprocess
{
    internal class PipeStreamWrapper
    {
        private const int MessageHeaderLengthBytes = 5;
        private readonly byte[] _headerReadBuffer = new byte[MessageHeaderLengthBytes];
        private readonly PipeStream _stream;
        // Prevents more than one thread from writing to the pipe stream at once
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public PipeStreamWrapper(PipeStream stream)
        {
            _stream = stream;
        }

        public IRequestHandler RequestHandler { get; set; } = null!;

        public IResponseHandler ResponseHandler { get; set; } = null!;

        public Task SendRequestAsync<T>(T request, CancellationToken cancellationToken)
        {
            return SendMessageAsync(InterprocessType.Request, request, cancellationToken);
        }

        public Task SendResponseAsync<T>(T response, CancellationToken cancellationToken)
        {
            return SendMessageAsync(InterprocessType.Response, response, cancellationToken);
        }

        private async Task SendMessageAsync<T>(InterprocessType interprocessType, T payloadObject, CancellationToken cancellationToken)
        {
            var payload = MessagePackSerializer.Serialize(payloadObject);
            var payloadLength = payload.Length;

            // First byte is the message type
            byte[] messageBytes = new byte[payloadLength + MessageHeaderLengthBytes];
            messageBytes[0] = (byte) interprocessType;

            // Next 4 bytes is the payload length
            byte[] payloadLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payloadLength));
            payloadLengthBytes.CopyTo(messageBytes, 1);

            // Rest is the payload
            payload.CopyTo(messageBytes, MessageHeaderLengthBytes);

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        ///     Processes the next message on the input stream.
        /// </summary>
        public async Task ProcessMessageAsync(CancellationToken cancellationToken)
        {
            var message = await ReadMessageAsync(cancellationToken);
            
            switch (message.messageType)
            {
                case InterprocessType.Request:
                    InterprocessRequest request = MessagePackSerializer.Deserialize<InterprocessRequest>(message.payload)!;
                    if (RequestHandler == null)
                    {
                        throw new InvalidOperationException("Request received but this endpoint is not set up to handle requests.");
                    }

                    RequestHandler.HandleRequest(request);
                    break;
                case InterprocessType.Response:
                    InterprocessResponse response = MessagePackSerializer.Deserialize<InterprocessResponse>(message.payload)!;
                    if (ResponseHandler == null)
                    {
                        throw new InvalidOperationException("Response received but this endpoint is not set up to make requests.");
                    }

                    ResponseHandler.HandleResponse(response);
                    break;
                default:
                    throw new InvalidOperationException($"Unrecognized message type: {message.messageType}");
            }
        }

        private async Task<(InterprocessType messageType, byte[] payload)> ReadMessageAsync(CancellationToken cancellationToken)
        {
            // Read the 5-byte header to see the message type and how long the message is
            var headerBytesRead = 0;
            while (headerBytesRead < MessageHeaderLengthBytes)
            {
                var readBytes = await _stream.ReadAsync(_headerReadBuffer, headerBytesRead, MessageHeaderLengthBytes - headerBytesRead, cancellationToken);
                if (readBytes == 0)
                {
                    ClosePipe();
                }

                headerBytesRead += readBytes;
            }

            var messageType = (InterprocessType) _headerReadBuffer[0];
            var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_headerReadBuffer, 1));

            byte[] payloadBytes = new byte[payloadLength];

            var payloadBytesRead = 0;
            while (payloadBytesRead < payloadLength)
            {
                var readBytes = await _stream.ReadAsync(payloadBytes, payloadBytesRead, payloadLength - payloadBytesRead, cancellationToken);
                if (readBytes == 0)
                {
                    ClosePipe();
                }
                payloadBytesRead += readBytes;
            }
            return (messageType, payloadBytes);
        }

        private void ClosePipe()
        {
            string message = "Pipe has closed.";
            throw new OperationCanceledException(message);
        }
    }
}