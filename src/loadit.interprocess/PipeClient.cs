using System;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Loadit.Interprocess
{
    public class PipeClient<TRequesting, THandling> : IDisposable
        where TRequesting : class
        where THandling : class
    {
        private readonly PipeMessageProcessor _messageProcessor = new();
        private NamedPipeClientStream? _rawPipeStream;
        private PipeStreamWrapper? _wrappedPipeStream;
        private bool _disposed;
        private MethodInvoker<TRequesting> _invoker = null!;

        public Task ConnectAsync(string pipeName, Func<THandling> handlerFactoryFunc, CancellationToken cancellationToken = default)
        {
            return ConnectAsync(pipeName, ".", handlerFactoryFunc, cancellationToken);
        }

        private async Task ConnectAsync(string pipeName, string serverName, Func<THandling> handlerFactoryFunc, CancellationToken cancellationToken = default)
        {
            _rawPipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _rawPipeStream.ConnectAsync(cancellationToken);
            _wrappedPipeStream = new PipeStreamWrapper(_rawPipeStream);
            _invoker = new MethodInvoker<TRequesting>(_wrappedPipeStream, _messageProcessor);
            // ReSharper disable once CA1806
            // ReSharper disable once ObjectCreationAsStatement
            new RequestHandler<THandling>(_wrappedPipeStream, handlerFactoryFunc!);
            _messageProcessor.StartProcessing(_wrappedPipeStream);
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
                _rawPipeStream?.Dispose();
            }

            _disposed = true;
        }

        public Task InvokeAsync(Expression<Action<TRequesting>> expression, CancellationToken cancellationToken = default)
        {
            return _invoker.InvokeAsync(expression, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}