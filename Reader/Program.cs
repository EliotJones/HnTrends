using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reader
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var entries = new List<Entry>();

            entries.AddRange(ReadFile(@"C:\git\csharp\hn-reader\hn.bin"));

            var others = Directory.GetFiles(@"C:\git\csharp\hn-reader");

            foreach (var file in others.Where(x => x.EndsWith("hn-complete.bin")))
            {
                entries.AddRange(ReadFile(file));
            }

            if (entries.Count == 0)
            {
                return;
            }

            entries = entries.OrderBy(x => x.Date).ToList();

            var min = entries[0].Date.Date;
            var max = entries[entries.Count - 1].Date.Date;

            var index = 0;

            var urlsSet = new HashSet<string>();
            var dateCounts = new Dictionary<DateTime, int>();
            var recessionCounts = new Dictionary<DateTime, int>();
            var teslaCounts = new Dictionary<DateTime, int>();

            while (min < max)
            {
                dateCounts[min] = 0;
                recessionCounts[min] = 0;
                teslaCounts[min] = 0;

                for (int i = index; i < entries.Count; i++)
                {
                    var e = entries[i];

                    if (e.Date.Date > min.Date)
                    {
                        break;
                    }

                    if (e.Title != null && e.Title.IndexOf("recession", StringComparison.OrdinalIgnoreCase) >= 0
                        && e.Url != null && !urlsSet.Contains(e.Url))
                    {
                        urlsSet.Add(e.Url);
                        recessionCounts[min]++;
                    }

                    if (e.Title?.IndexOf("tesla", StringComparison.OrdinalIgnoreCase) >= 0 && e.Url != null)
                    {
                        teslaCounts[min]++;
                    }

                    dateCounts[min]++;
                    index++;
                }

                min = min.AddDays(1);
            }

            using (var file = File.OpenWrite(@"C:\Temp\hn.csv"))
            using (var writer = new StreamWriter(file))
            {
                foreach (var date in dateCounts)
                {
                    var dateStr = $"{date.Key.Year}-{date.Key.Month}-{date.Key.Day}";
                    writer.WriteLine($"{dateStr},{dateCounts[date.Key]},{recessionCounts[date.Key]},{teslaCounts[date.Key]}");
                }
            }
        }

        private static IEnumerable<Entry> ReadFile(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            using (var reader = new BinaryReader(file))
            {
                while (file.Position < file.Length)
                {
                    var entry = Entry.Read(reader);

                    yield return entry;
                }
            }
        }
    }
}
