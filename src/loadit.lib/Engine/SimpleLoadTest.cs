using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loadit.Engine
{
    public class SimpleLoadTest<T> : ILoadTest
    {
        private readonly LoadOptions _options;
        private readonly T _runner;
        private readonly Func<T, CancellationToken, Task> _execute;

        public SimpleLoadTest(LoadOptions options, T runner, Func<T, CancellationToken, Task> execute)
        {
            _options = options;
            _runner = runner;
            _execute = execute;
        }
        
        public LoadOptions Options()
        {
            return _options;
        }

        public Task Setup(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task Run(CancellationToken token)
        {
            return _execute.Invoke(_runner, token);
        }

        public Task Teardown(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}