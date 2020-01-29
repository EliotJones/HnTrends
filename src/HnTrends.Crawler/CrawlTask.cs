namespace HnTrends.Crawler
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Core;
    using Database;
    using Newtonsoft.Json;

    public class CrawlTask
    {
        private static readonly Random Random = new Random(250);

        private readonly SQLiteConnection connection;
        private readonly HttpClient httpClient;

        public CrawlTask(SQLiteConnection connection, HttpClient httpClient)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task Run()
        {
            LastWriteTable.TryRead(connection, out var lastId);

            DateTime max;
            DateTime min;
            if (!DateRangeTable.TryRead(connection, out var range))
            {
                max = DateTime.MinValue;
                min = DateTime.MaxValue;
            }
            else
            {
                max = range.to;
                min = range.from;
            }
            
            var maxItem = int.Parse(await httpClient.GetStringAsync("https://hacker-news.firebaseio.com/v0/maxitem.json"));

            if (lastId >= maxItem)
            {
                return;
            }

            Trace.WriteLine($"Running from {lastId} to {maxItem}.");

            var entries = new List<Entry>();
            var errorCount = 0;

            for (var i = lastId + 1; i <= maxItem; i++)
            {
                try
                {
                    var url = $"https://hacker-news.firebaseio.com/v0/item/{i}.json";

                    var str = await httpClient.GetStringAsync(url);

                    if (str == null)
                    {
                        continue;
                    }

                    var item = JsonConvert.DeserializeObject<HnItem>(str);

                    if (item == null)
                    {
                        continue;
                    }

                    if (item.Type == "story" && item.Url != null && item.Title != null)
                    {
                        var entry = new Entry
                        {
                            Id = item.Id,
                            Time = item.Time,
                            Title = item.Title,
                            Url = item.Url,
                            Date = Entry.TimeToDate(item.Time)
                        };

                        entries.Add(entry);

                        Trace.WriteLine($"Saved Story: {i}.");

                        if (entries.Count % 5 == 0)
                        {
                            Trace.WriteLine("Flushing to database.");

                            WriteEntries(entries, i, ref min, ref max);

                            entries.Clear();
                        }
                    }

                    await Task.Delay(Random.Next(2, 5));

                }
                catch
                {
                    errorCount++;
                    await Task.Delay(1000);

                    if (errorCount >= 100)
                    {
                        break;
                    }
                }
            }

            if (entries.Count > 0)
            {
                WriteEntries(entries, maxItem, ref min, ref max);
            }
        }

        private void WriteEntries(IEnumerable<Entry> entries, int maxId, ref DateTime min, ref DateTime max)
        {
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var e in entries)
                {
                    StoryTable.Write(e, connection);

                    if (e.Date > max)
                    {
                        max = e.Date;
                    }

                    if (e.Date < min)
                    {
                        min = e.Date;
                    }
                }

                DateRangeTable.Write(min, max, connection);
                LastWriteTable.Write(maxId, connection);

                transaction.Commit();
            }
        }
    }
}