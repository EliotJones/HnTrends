using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    using System.Threading;

    public class HnItem
    {
        public int Id { get; set; }

        public long Time { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }

        public int[] Kids { get; set; }
    }

    public static class Program
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(3, 3);
        private static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://news.ycombinator.com/")
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
                var toLatestPath = Path.Combine(fileDirectory, "hn.bin");
                
                var maxItem = int.Parse(await client.GetStringAsync("https://hacker-news.firebaseio.com/v0/maxitem.json"));

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

                    await semaphore.WaitAsync();

                    var fileName = Path.Combine(fileDirectory, $"{nextStartItem}hn.bin");

                    if (File.Exists(Path.Combine(fileDirectory, $"{nextStartItem}hn-complete.bin")))
                    {
                        semaphore.Release();
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
                semaphore.Release();
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

                        var str = await client.GetStringAsync(url);

                        if (str == null)
                        {
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
