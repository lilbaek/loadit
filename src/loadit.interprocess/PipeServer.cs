using System;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Loadit.Interprocess
{
    public class PipeServer<TRequesting, THandling> : IDisposable
        where TRequesting : class
        where THandling : class
    {
        private readonly ILogger _logger;
        private readonly PipeMessageProcessor _messageProcessor = new();
        private NamedPipeServerStream _rawPipeStream = null!;
        private bool _disposed;
        private MethodInvoker<TRequesting> _invoker = null!;

        public PipeServer(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("PipeServer");
        }

        public async Task WaitForConnectionAsync(string pipeName, Func<THandling> handlerFactoryFunc, CancellationToken cancellationToken = default)
        {
            _rawPipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            _logger.LogDebug($"Set up named pipe server '{pipeName}'.");
            await _rawPipeStream.WaitForConnectionAsync(cancellationToken);
            _logger.LogDebug("Connected to client.");

            var wrappedPipeStream = new PipeStreamWrapper(_rawPipeStream);
            _invoker = new MethodInvoker<TRequesting>(wrappedPipeStream, _messageProcessor);
            // ReSharper disable once CA1806
            // ReSharper disable once ObjectCreationAsStatement
            new RequestHandler<THandling>(wrappedPipeStream, handlerFactoryFunc);

            _messageProcessor.StartProcessing(wrappedPipeStream);
        }

        public Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
        {
            return _invoker.InvokeAsync(expression, cancellationToken);
        }

        public Task<T> InvokeAsync<T>(Expression<Func<TRequesting, T>> expression, CancellationToken cancellationToken = default)
        {
            return _invoker.InvokeAsync(expression, cancellationToken);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _messageProcessor.Dispose();
                _rawPipeStream.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}