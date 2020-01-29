namespace HnTrends.ConsoleCrawler
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Crawler;
    using Database;

    public static class Program
    {
        private static readonly HttpClient Client = new HttpClient()
        {
            BaseAddress = new Uri("https://news.ycombinator.com/"),
            Timeout = TimeSpan.FromMinutes(1)
        };

        public static async Task Main(string[] args)
        {
            Trace.Listeners.Add(MyConsoleListener.Instance);
            
            using (var connection = Connector.ConnectToFile(@"C:\git\csharp\hn-reader\data\hn-data.sqlite"))
            {
                var crawlTask = new CrawlTask(connection, Client);
                
                await crawlTask.Run();
            }
        }
    }
}
