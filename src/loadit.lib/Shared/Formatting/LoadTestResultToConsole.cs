using System;
using System.Linq;
using Humanizer;
using loadit.shared.Result;
using Spectre.Console;

namespace loadit.shared.Formatting
{
    public static class LoadTestResultToConsole
    {
        public static void Report(SummarizedResult result)
        {
            var table = new Table();
            table.AddColumn("Metric");
            table.AddColumn("");
            table.AddColumn("");
            table.HideHeaders();
            table.Border(TableBorder.Ascii);

            table.AddRow("iterations", St($"{result.Iterations:0}"), St($"{result.IterationsPrSecond:0}/s"));
            table.AddRow("http requests", St($"{result.HttpRequests:0}"), St($"{result.HttpRequestsPrSecond:0}/s"));
            table.AddRow("http errors", St($"{result.HttpErrors:0}"), "");
            table.AddRow("data received", St($"{result.BytesReceived.Bytes().ToString("#.#")}"), St($"{result.BytesReceivedPrSecond.Bytes().ToString("#.#")}/s"));
            table.AddRow("data sent", St($"{result.BytesSent.Bytes().ToString("#.#")}"), St($"{result.BytesSentPrSecond.Bytes().ToString("#.#")}/s ({result.Bandwidth} mbit)"));
            table.AddEmptyRow();
            foreach (var timing in result.Timings.Where(timing => timing.Max != 0))
            {
                table.AddRow($"{timing.Name}:", StTiming("avg", timing.Avg, 10), FormatTiming(timing));
            }
            AnsiConsole.Render(table);
            AnsiConsole.MarkupLine($"[dim deepskyblue2]Done in {result.Elapsed.Humanize(5)}[/]");
        }

        private static string St(string text)
        {
            return $"[deepskyblue2]{text}[/]";
        }

        private static string FormatTiming(ResponseTimeResult timing)
        {
            return StTiming("min", timing.Min) + StTiming("med", timing.Median) + StTiming("max", timing.Max) + StTiming("P(90)", timing.P90) + StTiming("P(95)", timing.P95);
        }

        private static string StTiming(string name, double value, int pad = 15)
        {
            var timeSpan = TimeSpan.FromMilliseconds(value);
            if (value < 0.1)
            {
                var microSeconds = timeSpan.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                return Wrap($"{name}={microSeconds:0.00}μs");
            }
            if (value >= 1000)
            {
                return Wrap($"{name}={timeSpan.TotalSeconds:0.00}s");    
            }
            return Wrap($"{name}={value:0.00}ms");

            string Wrap(string str)
            {
                return $"[deepskyblue2]{str.PadRight(pad, Char.Parse(" "))}[/]";
            }
        }
    }
}