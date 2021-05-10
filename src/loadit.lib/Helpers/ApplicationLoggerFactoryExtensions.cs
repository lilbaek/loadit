using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Loadit.Progress
{
    public static class ApplicationLoggerFactoryExtensions
    {
        public static ILoggingBuilder ProgressLogger(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, ProgressLoggerProvider>();
            //Be careful here. Singleton may not be OK for multi tenant applications - You can try and use Transient instead.
            return builder;
        }
    }
}