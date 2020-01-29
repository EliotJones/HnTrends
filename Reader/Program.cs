namespace Reader
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press 1 to write to CSV or 2 to write to SQLite.");

            var mode = Console.ReadKey().KeyChar;

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

            if (mode == '1')
            {
                Console.WriteLine("Writing to file.");

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
            else if (mode == '2')
            {
                Console.WriteLine("Writing to SQLite");

                const string filePath = @"C:\temp\hn-data.sqlite";
                var isNew = !File.Exists(filePath);
                using (var connection = new SQLiteConnection($"Data Source={filePath}"))
                {
                    connection.Open();
                    if (isNew)
                    {
                        var createCommand = new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS story (
    id INTEGER PRIMARY KEY,
    title TEXT NULL COLLATE NOCASE,
    url TEXT NULL COLLATE NOCASE,
    ticks INTEGER
);

CREATE INDEX ix_ticks ON story (ticks);", connection);

                        createCommand.ExecuteNonQuery();
                    }

                    var num = 0;
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var entry in entries)
                        {
                            var command = new SQLiteCommand(@"INSERT OR IGNORE INTO 
story(id, title, url, ticks) VALUES(@id, @title, @url, @ticks);", connection);
                            command.Parameters.AddWithValue("id", entry.Id);
                            command.Parameters.AddWithValue("title", entry.Title);
                            command.Parameters.AddWithValue("url", entry.Url);
                            command.Parameters.AddWithValue("ticks", entry.Date.Ticks);

                            command.ExecuteNonQuery();
                            num++;

                            if (num % 10_000 == 0)
                            {
                                Console.WriteLine($"Inserted {num} rows.");
                            }
                        }

                        transaction.Commit();
                    }
                }

                Console.WriteLine("Insert complete.");
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
