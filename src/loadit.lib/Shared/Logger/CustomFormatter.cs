using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace loadit.shared.Logger
{
    public class CustomFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable _optionsReloadToken;
        
        public CustomFormatter(IOptionsMonitor<CustomOptions> options)
            // Case insensitive
            : base("Spectre") =>
            (_optionsReloadToken, _) =
            (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(CustomOptions options)
        {
        }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider scopeProvider,
            TextWriter textWriter)
        {
            var message = logEntry.Formatter(logEntry.State, null);
            if (logEntry.Exception != null)
            {
                AnsiConsole.Render(new Markup(GetText(logEntry.LogLevel, message)));
                AnsiConsole.WriteLine();
                AnsiConsole.WriteException(logEntry.Exception);
                AnsiConsole.WriteLine();
                return;
            }
            AnsiConsole.Render(new Markup(GetText(logEntry.LogLevel, message)));
            AnsiConsole.WriteLine();
        }

        private string GetText(LogLevel level, string message)
        {
            return level switch
            {
                LogLevel.Trace => $"[italic dim grey]{message}[/]",
                LogLevel.Debug => $"[dim grey]{message}[/]",
                LogLevel.Information => $"[dim deepskyblue2]{message}[/]",
                LogLevel.Warning => $"[bold orange3]{message}[/]",
                LogLevel.Error => $"[bold red]{message}[/]",
                LogLevel.Critical => $"[bold underline red on white]{message}[/]",
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };
        }

        public void Dispose() => _optionsReloadToken?.Dispose();
    }
}