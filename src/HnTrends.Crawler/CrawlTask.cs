namespace HnTrends.Crawler
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Database;
    using Newtonsoft.Json;

    public class CrawlTask
    {
        private const int ThreadBucketSize = 30;

        private static readonly Random Random = new Random(250);

        private readonly SQLiteConnection connection;
        private readonly HttpClient httpClient;
        private readonly byte maxThreads;

        public CrawlTask(SQLiteConnection connection, HttpClient httpClient, byte maxThreads)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (maxThreads < 1)
            {
                maxThreads = 1;
            }

            this.maxThreads = maxThreads;
        }

        public async Task Run(CancellationToken cancellationToken = default(CancellationToken))
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

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var maxItem = int.Parse(await httpClient.GetStringAsync("https://hacker-news.firebaseio.com/v0/maxitem.json"));

            if (lastId >= maxItem)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var count = maxItem - Math.Max(0, lastId);
            Trace.WriteLine($"Running from {lastId} to {maxItem} ({count} items).");

            var entries = new ConcurrentBag<Entry>();

            var interval = (ThreadBucketSize * maxThreads);

            for (var i = lastId + 1; i <= maxItem; i += interval)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var tasks = new Task[maxThreads];
                for (var threadIndex = 0; threadIndex < maxThreads; threadIndex++)
                {
                    var offset = threadIndex * ThreadBucketSize;

                    var threadStartId = i + offset;

                    var task = RunBucket(threadStartId, threadStartId + ThreadBucketSize, entries, cancellationToken);

                    tasks[threadIndex] = task;
                }

                await Task.WhenAll(tasks);

                if (entries.Count > 10)
                {
                    Trace.WriteLine("Flushing to database.");

                    WriteEntries(entries.OrderBy(x => x.Id), i + interval - 1, ref min, ref max);

                    entries.Clear();
                }
            }

            if (entries.Count > 0)
            {
                WriteEntries(entries.OrderBy(x => x.Id), maxItem, ref min, ref max);
            }
        }

        private async Task RunBucket(int from, int to,
            ConcurrentBag<Entry> entries, CancellationToken token)
        {
            for (var i = from; i < to; i++)
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
                }

                if (i < to)
                {
                    await Task.Delay(Random.Next(1, 5), token);
                }
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