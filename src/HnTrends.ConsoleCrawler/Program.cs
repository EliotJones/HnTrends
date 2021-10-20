namespace HnTrends.ConsoleCrawler
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Crawler;
    using Database;
    using Microsoft.Data.Sqlite;

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

            var connectionFactory = new MyConnectionFactory(@"C:\git\csharp\hn-reader\data\hn-data.sqlite");

            var crawlTask = new CrawlTask(connectionFactory, Client, 3);

            await crawlTask.Run();
        }

        private class MyConnectionFactory : IConnectionFactory
        {
            private readonly string connectionString;

            public MyConnectionFactory(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public SqliteConnection Open()
            {
                var connection = Connector.ConnectToFile(connectionString);

                connection.Open();

                return connection;
            }
        }
    }
}
