using System;
using System.IO;

namespace Loadit.Interprocess
{
    public class PipeInvokeFailedException : IOException
    {
        public PipeInvokeFailedException(string message)
            : base(message)
        {
        }
    }
}