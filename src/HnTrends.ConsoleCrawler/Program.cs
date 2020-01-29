namespace HnTrends.ConsoleCrawler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public static class Program
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(3, 3);

        private static readonly HttpClient Client = new HttpClient()
        {
            BaseAddress = new Uri("https://news.ycombinator.com/"),
            Timeout = TimeSpan.FromMinutes(1)
        };

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Choose a run mode, 1) to-latest, 2) backfill: ");
            var mode = int.Parse(Console.ReadKey().KeyChar.ToString());

            const int binSize = 250_000;
            const int toLatestStartItem = 15000000;
            const string fileDirectory = @"C:\git\csharp\hn-reader\";

            var random = new Random();

            if (mode == 1)
            {
                Console.WriteLine();
                Console.WriteLine("Running to latest item.");
                Console.WriteLine();

                var toLatestPath = Path.Combine(fileDirectory, "hn.bin");
                
                var maxItem = int.Parse(await Client.GetStringAsync("https://hacker-news.firebaseio.com/v0/maxitem.json"));

                Console.WriteLine($"Highest item ID is: {maxItem}.");

                await RunFile(toLatestPath, toLatestStartItem, maxItem, random);
            }
            else if (mode == 2)
            {
                var previousStartItem = toLatestStartItem;
                var nextStartItem = previousStartItem;

                var runningTasks = new List<Task>();

                while (nextStartItem > 0)
                {
                    previousStartItem = nextStartItem;
                    nextStartItem -= binSize;

                    await Semaphore.WaitAsync();

                    var fileName = Path.Combine(fileDirectory, $"{nextStartItem}hn.bin");

                    if (File.Exists(Path.Combine(fileDirectory, $"{nextStartItem}hn-complete.bin")))
                    {
                        Semaphore.Release();
                        continue;
                    }

                    var task = RunBucket(fileName, nextStartItem, previousStartItem, random);

                    runningTasks.Add(task);
                }

                await Task.WhenAll(runningTasks);
            }
        }

        private static async Task RunBucket(string filepath, int from, int to, Random random)
        {
            try
            {
                await RunFile(filepath, from, to, random);
                File.Move(filepath, filepath.Replace("hn.bin", "hn-complete.bin"));
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static async Task RunFile(string filepath, int from, int to, Random random)
        {
            var currentItem = from;

            if (File.Exists(filepath))
            {
                using (var file = File.OpenRead(filepath))
                using (var reader = new BinaryReader(file))
                {
                    while (file.Position < file.Length)
                    {
                        try
                        {
                            var id = reader.ReadInt32();
                            var text = reader.ReadString();
                            var url = reader.ReadString();
                            var time = reader.ReadInt64();

                            currentItem = id;
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                }
            }

            var skipped = new HashSet<int>();

            var storyCount = 0;

            var errorCount = 0;

            Console.WriteLine($"Starting at: {currentItem}.");

            using (var file = File.Open(filepath, FileMode.Append))
            using (var binaryWriter = new BinaryWriter(file))
            {
                while (currentItem < to)
                {
                    try
                    {
                        var url = $"https://hacker-news.firebaseio.com/v0/item/{currentItem}.json";

                        var str = await Client.GetStringAsync(url);

                        if (str == null || str == "null")
                        {
                            currentItem++;
                            continue;
                        }

                        var item = JsonConvert.DeserializeObject<HnItem>(str);

                        if (item.Type == "story" && item.Url != null && item.Title != null)
                        {
                            binaryWriter.Write(item.Id);
                            binaryWriter.Write(item.Title);
                            binaryWriter.Write(item.Url);
                            binaryWriter.Write(item.Time);

                            if (item.Kids != null)
                            {
                                foreach (var kid in item.Kids)
                                {
                                    skipped.Add(kid);
                                }
                            }

                            Console.WriteLine($"Saved Story: {++storyCount}.");

                            if (storyCount % 5 == 0)
                            {
                                Console.WriteLine("Flushing to file.");
                                binaryWriter.Flush();
                                file.Flush();
                            }
                        }

                        await Task.Delay(random.Next(2, 5));

                        while (skipped.Contains(currentItem++))
                        {
                        }
                    }
                    catch
                    {
                        errorCount++;
                        currentItem++;
                        await Task.Delay(1000);

                        if (errorCount >= 100)
                        {
                            break;
                        }
                    }
                }

                binaryWriter.Flush();
                file.Flush();
            }
        }
    }
}
