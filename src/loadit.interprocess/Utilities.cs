using System;
using System.IO;
using Loadit.Interprocess.Models;
using MessagePack;

namespace Loadit.Interprocess
{
    internal static class Utilities
    {
        public static bool TryConvert(object valueToConvert, Type targetType, out object? targetValue)
        {
            if (targetType.IsInstanceOfType(valueToConvert))
            {
                targetValue = valueToConvert;
                return true;
            }

            if (targetType.IsEnum)
            {
                if (valueToConvert is string str)
                {
                    try
                    {
                        targetValue = Enum.Parse(targetType, str, true);
                        return true;
                    }
                    catch
                    {
                        //Ignore
                    }
                }
                else
                {
                    try
                    {
                        targetValue = Enum.ToObject(targetType, valueToConvert);
                        return true;
                    }
                    catch
                    {
                        //Ignore
                    }
                }
            }

            if (valueToConvert is string string2 && targetType == typeof(Guid))
            {
                if (Guid.TryParse(string2, out var result))
                {
                    targetValue = result;
                    return true;
                }
            }
            try
            {
                targetValue = Convert.ChangeType(valueToConvert, targetType);
                return true;
            }
            catch
            {
                //Ignore
            }
            
            try
            {
                targetValue = MessagePackSerializer.Deserialize(targetType, MessagePackSerializer.Serialize(valueToConvert));
                return true;
            }
            catch
            {
                //Ignore
            }
            
            targetValue = null;
            return false;
        }

        /// <summary>
        ///     Ensures the pipe state is ready to invoke methods.
        /// </summary>
        public static void EnsureReadyForInvoke(PipeState state, Exception pipeFault)
        {
            if (state == PipeState.NotOpened)
            {
                throw new IOException("Can only invoke methods after connecting the pipe.");
            }

            if (state == PipeState.Closed)
            {
                throw new IOException("Cannot invoke methods after the pipe has closed.");
            }

            if (state == PipeState.Faulted)
            {
                throw new IOException("Cannot invoke method. Pipe has faulted.", pipeFault);
            }
        }
    }
}