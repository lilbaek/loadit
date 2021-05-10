using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Interprocess.Models;

namespace Loadit.Interprocess
{
    internal class MethodInvoker<T> : IResponseHandler
        where T : class
    {
        private readonly PipeMessageProcessor _pipeHost;
        private readonly PipeStreamWrapper _pipeStreamWrapper;

        private long _currentCall;
        private readonly Dictionary<long, PendingCall> _pendingCalls = new();

        // Lock object for accessing pending calls dictionary.
        private readonly object _pendingCallsLock = new();

        public MethodInvoker(PipeStreamWrapper pipeStreamWrapper, PipeMessageProcessor pipeHost)
        {
            _pipeStreamWrapper = pipeStreamWrapper;
            _pipeStreamWrapper.ResponseHandler = this;

            _pipeHost = pipeHost;
        }

        public async Task InvokeAsync(Expression<Action<T>> expression, CancellationToken cancellationToken = default)
        {
            Utilities.EnsureReadyForInvoke(_pipeHost.State, _pipeHost.PipeFault);
            InterprocessResponse response = await GetResponseFromExpressionAsync(expression, cancellationToken);
            if (!response.Succeeded)
            {
                throw new PipeInvokeFailedException(response.Error);
            }
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<T, TResult>> expression, CancellationToken cancellationToken = default)
        {
            Utilities.EnsureReadyForInvoke(_pipeHost.State, _pipeHost.PipeFault);
            var response = await GetResponseFromExpressionAsync(expression, cancellationToken).ConfigureAwait(false);
            if (response.Succeeded)
            {
                if (Utilities.TryConvert(response.Data, typeof(TResult), out object? result))
                {
                    return (TResult) result!;
                }

                throw new InvalidOperationException($"Unable to convert returned value to '{typeof(TResult).Name}'.");
            }

            throw new PipeInvokeFailedException(response.Error);
        }

        public void HandleResponse(InterprocessResponse response)
        {
            PendingCall? pendingCall;
            lock (_pendingCallsLock)
            {
                if (_pendingCalls.TryGetValue(response.CallId, out pendingCall))
                {
                    // Call has completed. Remove from pending list.
                    _pendingCalls.Remove(response.CallId);
                }
                else
                {
                    throw new InvalidOperationException($"No pending call {response.CallId}");
                }
            }
            // Mark method call task as completed.
            pendingCall.TaskCompletionSource.TrySetResult(response);
        }

        private async Task<InterprocessResponse> GetResponseFromExpressionAsync(Expression expression, CancellationToken cancellationToken)
        {
            InterprocessRequest request = CreateRequest(expression);
            return await GetResponseAsync(request, cancellationToken);
        }

        private InterprocessRequest CreateRequest(Expression expression)
        {
            if (!(expression is LambdaExpression lamdaExp))
            {
                throw new ArgumentException("Only supports lambda expressions");
            }

            if (!(lamdaExp.Body is MethodCallExpression methodCallExp))
            {
                throw new ArgumentException("Only supports calling methods");
            }
            var callId = Interlocked.Increment(ref _currentCall);
            return new InterprocessRequest
            {
                CallId = callId,
                MethodName = methodCallExp.Method.Name,
                GenericArguments = methodCallExp.Method.GetGenericArguments(),
                Parameters = methodCallExp.Arguments.Select(argumentExpression => Expression.Lambda(argumentExpression).Compile().DynamicInvoke()).ToArray()!
            };
        }

        private async Task<InterprocessResponse> GetResponseAsync(InterprocessRequest request, CancellationToken cancellationToken)
        {
            var pendingCall = new PendingCall();

            lock (_pendingCallsLock)
            {
                _pendingCalls.Add(request.CallId, pendingCall);
            }

            await _pipeStreamWrapper.SendRequestAsync(request, cancellationToken);

            cancellationToken.Register(
                () => { pendingCall.TaskCompletionSource.TrySetException(new OperationCanceledException("Request has been canceled.")); },
                false);

            return await pendingCall.TaskCompletionSource.Task;
        }
    }
}