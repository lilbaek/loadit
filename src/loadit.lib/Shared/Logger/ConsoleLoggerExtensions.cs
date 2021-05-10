using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace loadit.shared.Logger
{
    public static class ConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddSpectreFormatter(
            this ILoggingBuilder builder) =>
            builder.AddConsole(options => options.FormatterName = "Spectre")
                .AddConsoleFormatter<CustomFormatter, CustomOptions>();
    }
    
    public class CustomOptions : ConsoleFormatterOptions
    {

    }
}