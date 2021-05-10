using System;
using System.Threading;
using Loadit.Interprocess.Models;

namespace Loadit.Interprocess
{
    internal sealed class PipeMessageProcessor : IDisposable
    {
        private CancellationTokenSource? _workLoopCancellationTokenSource;
        private bool _disposed;
        public PipeState State { get; private set; } = PipeState.NotOpened;
        public Exception PipeFault { get; private set; } = null!;

        public async void StartProcessing(PipeStreamWrapper pipeStreamWrapper)
        {
            if (State != PipeState.NotOpened)
            {
                throw new InvalidOperationException("Can only call connect once");
            }

            State = PipeState.Connected;

            try
            {
                _workLoopCancellationTokenSource = new CancellationTokenSource();

                // Process messages until canceled.
                while (true)
                {
                    _workLoopCancellationTokenSource.Token.ThrowIfCancellationRequested();
                    await pipeStreamWrapper.ProcessMessageAsync(_workLoopCancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // This is a normal dispose.
                State = PipeState.Closed;
            }
            catch (Exception exception)
            {
                State = PipeState.Faulted;
                PipeFault = exception;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _workLoopCancellationTokenSource?.Cancel();
                _disposed = true;
            }
        }
    }
}