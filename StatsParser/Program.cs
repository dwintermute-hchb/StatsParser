using System;
using System.Xml.Linq;

namespace StatsParser // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        private const string ApplicationNameFilter = "Microsoft JDBC Driver for SQL Server";

        static async Task Main(string[] args)
        {
            var parsed = await ParseFile(args[0]);
            var filtered = parsed.Where(p => p.TextData.Contains("declare") && p.Cpu > 0).ToList();
            var summary = BuildSummary(filtered);

            PrintSummary(summary);
        }

        private static async Task<IEnumerable<PerformanceEvent>> ParseFile(string filename)
        {
            var xml = XElement.Load(filename);
            var ns = xml.GetDefaultNamespace();

            var sqlEvents = xml.Elements(NamespaceNode(ns, "Events")).First().Elements();

            var parsedEvents = new List<PerformanceEvent>();

            foreach (var sqlEvent in sqlEvents)
            {
                if (IsRelevantEvent(sqlEvent, ns))
                {
                    parsedEvents.Add(ParseEvent(sqlEvent, ns));
                }
            }

            return parsedEvents;
        }

        private static XName NamespaceNode(XNamespace ns, string node)
        {
            return ns + node;
        }

        private static bool IsRelevantEvent(XElement element, XNamespace ns)
        {
            var columns = element.Elements(NamespaceNode(ns, "Column"));

            var appNameColumns = columns.Where(e => e.Attributes().Any(a => a.Name == "name" && a.Value == "ApplicationName"));

            return appNameColumns.Count() == 1 
                && appNameColumns.First().Value == ApplicationNameFilter;
        }

        private static PerformanceEventsSummary BuildSummary(IEnumerable<PerformanceEvent> events)
        {
            var summary = new PerformanceEventsSummary(
                MinCpu: events.Min(e => e.Cpu),
                MaxCpu: events.Max(e => e.Cpu),
                MinDuration: events.Min(e => e.Duration),
                MaxDuration: events.Max(e => e.Duration),
                AverageReads: events.Average(e => e.Reads),
                AverageWrites: events.Average(e => e.Writes),
                AverageCpu: events.Average(e => e.Cpu),
                AverageDuration: events.Average(e => e.Duration),
                SampleSize: events.Count()
            );

            return summary;
        }

        private static void PrintSummary(PerformanceEventsSummary summary)
        {
            Console.WriteLine(GetPrintableLine("Sample Size", summary.SampleSize.ToString("F")));

            Console.WriteLine(GetPrintableLine("Min CPU", summary.MinCpu.ToString("F")));
            Console.WriteLine(GetPrintableLine("Max CPU", summary.MaxCpu.ToString("F")));
            Console.WriteLine(GetPrintableLine("Average CPU", summary.AverageCpu.ToString("F")));

            Console.WriteLine(GetPrintableLine("Min Duration", summary.MinDuration.ToString("F")));
            Console.WriteLine(GetPrintableLine("Max Duration", summary.MaxDuration.ToString("F")));
            Console.WriteLine(GetPrintableLine("Average Duration", summary.AverageDuration.ToString("F")));

            Console.WriteLine(GetPrintableLine("Average Reads", summary.AverageReads.ToString("F")));
            Console.WriteLine(GetPrintableLine("Average Writes", summary.AverageWrites.ToString("F")));
        }

        private static string GetPrintableLine(string label, string value)
        {
            return $"{label.PadLeft(20, ' ')}:{value.PadLeft(20, ' ')}";
        }

        private static PerformanceEvent ParseEvent(XElement element, XNamespace ns)
        {
            var newEvent = new PerformanceEvent();

            foreach (var column in element.Elements(NamespaceNode(ns, "Column"))) 
            {
                var columnType = GetColumnType(column);

                if (columnType == "Duration")
                {
                    newEvent.Duration = int.Parse(column.Value);
                }
                else if (columnType == "CPU")
                {
                    newEvent.Cpu = int.Parse(column.Value);
                }
                else if (columnType == "Reads")
                {
                    newEvent.Reads = int.Parse(column.Value);
                }
                else if (columnType == "Writes")
                {
                    newEvent.Writes = int.Parse(column.Value);
                }
                else if (columnType == "TextData")
                {
                    newEvent.TextData = column.Value;
                }
            }

            return newEvent;
        }

        private static string GetColumnType(XElement column)
        {
            return column.Attributes().First(a => a.Name == "name").Value;
        }

        private class PerformanceEvent
        {
            public string ApplicationName { get; set; } = string.Empty;
            public string TextData { get; set; } = string.Empty;
            public string LoginName { get; set; } = string.Empty;
            public int Duration { get; set; }
            public int Cpu { get; set; }
            public int Reads { get; set; }
            public int Writes { get; set; }
        }

        private readonly record struct PerformanceEventsSummary(
            int MinCpu,
            int MinDuration,
            int MaxDuration,
            int MaxCpu,
            double AverageDuration,
            double AverageCpu,
            double AverageReads,
            double AverageWrites,
            int SampleSize
            );
    }
}


