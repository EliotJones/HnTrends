namespace HnTrends
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class TrendService : ITrendService
    {
        private static readonly object Locker = new object();

        private readonly string dataDirectory = @"C:\git\csharp\hn-reader\";

        private readonly Dictionary<string, DailyTrendData> cache = new Dictionary<string, DailyTrendData>(StringComparer.OrdinalIgnoreCase);

        public DailyTrendData GetTrendDataForTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException();
            }

            lock (Locker)
            {
                if (cache.TryGetValue(searchTerm, out var cached))
                {
                    return cached;
                }

                var files = Directory.GetFiles(dataDirectory);
                
                var urls = new HashSet<string>();
                var counts = new Dictionary<DateTime, int>();
                var dateTotals = new Dictionary<DateTime, int>();

                var min = DateTime.MaxValue;
                var max = DateTime.MinValue;

                foreach (var file in files)
                {
                    if (!file.EndsWith("hn-complete.bin") && Path.GetFileName(file) != "hn.bin")
                    {
                        continue;
                    }
                    
                    using (var fileStream = File.OpenRead(file))
                    using (var reader = new BinaryReader(fileStream))
                    {
                        while (fileStream.Position < fileStream.Length)
                        {
                            var entry = Entry.Read(reader);

                            if (entry.Title == null || entry.Url == null)
                            {
                                continue;
                            }

                            if (!dateTotals.ContainsKey(entry.Date.Date))
                            {
                                dateTotals[entry.Date.Date] = 1;
                            }
                            else
                            {
                                dateTotals[entry.Date.Date]++;
                            }

                            if (entry.Date.Date < min)
                            {
                                min = entry.Date.Date;
                            }

                            if (entry.Date.Date > max)
                            {
                                max = entry.Date.Date;
                            }

                            if (entry.Title.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
                                || entry.Url.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (urls.Contains(entry.Url))
                                {
                                    continue;
                                }

                                if (!counts.ContainsKey(entry.Date.Date))
                                {
                                    counts[entry.Date.Date] = 1;
                                }
                                else
                                {
                                    counts[entry.Date.Date]++;
                                }

                                urls.Add(entry.Url);
                            }
                        }
                    }
                }

                var countMax = 0;
                var countsNum = new List<int>(counts.Count);
                var dates = new List<DateTime>(counts.Count);
                var percents = new List<double>();
                foreach (var pair in counts.OrderBy(x => x.Key))
                {
                    countsNum.Add(pair.Value);
                    dates.Add(pair.Key);
                    percents.Add((pair.Value / (double)dateTotals[pair.Key]) * 100.0);

                    if (pair.Value > countMax)
                    {
                        countMax = pair.Value;
                    }
                }

                var result = new DailyTrendData
                {
                    Counts = countsNum,
                    Dates = dates,
                    End = max,
                    Start = min,
                    CountMax = countMax,
                    Percents = percents
                };

                cache[searchTerm] = result;

                return result;
            }
        }

        public DailyTotalData GetDailyTotalData()
        {
            throw new NotImplementedException();
        }
    }

    public interface ITrendService
    {
        DailyTrendData GetTrendDataForTerm(string searchTerm);

        DailyTotalData GetDailyTotalData();
    }

    public class DailyTrendData
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public List<double> Percents { get; set; }

        public List<int> Counts { get; set; }

        public int CountMax { get; set; }

        public List<DateTime> Dates { get; set; }
    }

    public class DailyTotalData
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public ushort[] CountPerDay { get; set; }
    }
}
