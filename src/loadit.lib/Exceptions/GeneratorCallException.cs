using System;

namespace Loadit.Exceptions
{
    public class GeneratorCallException : Exception
    {
        public GeneratorCallException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}