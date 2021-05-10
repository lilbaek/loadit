using System;

namespace Loadit.Tool.Runner
{
    public class RunException : Exception
    {
        public RunException(string? message) : base(message)
        {
        }
    }
}