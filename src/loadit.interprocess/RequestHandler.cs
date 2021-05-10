using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Interprocess.Models;

namespace Loadit.Interprocess
{
    internal class RequestHandler<T> : IRequestHandler
    {
        private readonly Func<T> _handlerFactoryFunc;
        private readonly PipeStreamWrapper _pipeStreamWrapper;

        public RequestHandler(PipeStreamWrapper pipeStreamWrapper, Func<T> handlerFactoryFunc)
        {
            _pipeStreamWrapper = pipeStreamWrapper;
            _handlerFactoryFunc = handlerFactoryFunc;

            _pipeStreamWrapper.RequestHandler = this;
        }

        public async void HandleRequest(InterprocessRequest request)
        {
            try
            {
                InterprocessResponse response = await HandleRequestAsync(request);
                await _pipeStreamWrapper.SendResponseAsync(response, CancellationToken.None);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private async Task<InterprocessResponse> HandleRequestAsync(InterprocessRequest request)
        {
            try
            {
                var handlerInstance = _handlerFactoryFunc();
                if (handlerInstance == null)
                {
                    return GetFailure($"Handler implementation returned null for interface '{typeof(T).FullName}'");
                }

                var method = handlerInstance.GetType().GetMethod(request.MethodName);
                if (method == null)
                {
                    return GetFailure($"Method '{request.MethodName}' not found in interface '{typeof(T).FullName}'.");
                }

                ParameterInfo[] paramInfoList = method.GetParameters();
                if (paramInfoList.Length != request.Parameters.Length)
                {
                    return GetFailure($"Parameter count mismatch for method '{request.MethodName}'.");
                }

                Type[] genericArguments = method.GetGenericArguments();
                if (genericArguments.Length != request.GenericArguments.Length)
                {
                    return GetFailure($"Generic argument count mismatch for method '{request.MethodName}'.");
                }

                if (paramInfoList.Any(info => info.IsOut || info.ParameterType.IsByRef))
                {
                    return GetFailure($"Ref parameters are not supported. Method: '{request.MethodName}'");
                }


                object?[] args = new object[paramInfoList.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    object origValue = request.Parameters[i];
                    Type destType = paramInfoList[i].ParameterType;
                    if (destType.IsGenericParameter)
                    {
                        destType = request.GenericArguments[destType.GenericParameterPosition];
                    }

                    if (Utilities.TryConvert(origValue, destType, out object? arg))
                    {
                        args[i] = arg;
                    }
                    else
                    {
                        return new InterprocessResponse()
                        {
                            CallId = request.CallId,
                            Succeeded = false,
                            Error = $"Cannot convert value of parameter '{paramInfoList[i].Name}' ({origValue}) from {origValue.GetType().Name} to {destType.Name}."
                        };
                    }
                }

                try
                {
                    if (method.IsGenericMethod)
                    {
                        method = method.MakeGenericMethod(request.GenericArguments);
                    }

                    object result = method.Invoke(handlerInstance, args)!;

                    if (result is Task task)
                    {
                        await task;
                        var resultProperty = task.GetType().GetProperty("Result");
                        return new InterprocessResponse
                        {
                            Succeeded = true,
                            CallId = request.CallId,
                            Data = resultProperty?.GetValue(task)!
                        };
                    }
                    return new InterprocessResponse
                    {
                        Succeeded = true,
                        CallId = request.CallId,
                        Data = result
                    };
                }
                catch (Exception exception)
                {
                    return new InterprocessResponse
                    {
                        Succeeded = false,
                        CallId = request.CallId,
                        Error = exception.ToString()
                    };
                }
            }
            catch (Exception e)
            {
                return new InterprocessResponse
                {
                    Succeeded = false,
                    CallId = request.CallId,
                    Error = e.ToString()
                };
            }

            InterprocessResponse GetFailure(string message)
            {
                return new()
                {
                    CallId = request.CallId,
                    Succeeded = false,
                    Error = message
                };
            }
        }
    }
}