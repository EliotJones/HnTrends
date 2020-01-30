namespace HnTrends.Services
{
    using System;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Caches;
    using Crawler;
    using Indexer;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Responsible for scheduled downloading of data and re-indexing.
    /// </summary>
    internal class UpdateDataBackgroundService : BackgroundService
    {
        private readonly IIndexManager indexManager;
        private readonly SQLiteConnection connection;
        private readonly HttpClient client;
        private readonly ICacheManager cacheManager;
        private readonly ILogger<UpdateDataBackgroundService> logger;
        private readonly uint minutesBetweenRun;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public UpdateDataBackgroundService(IIndexManager indexManager,
            SQLiteConnection connection,
            HttpClient client,
            ICacheManager cacheManager,
            IOptions<TimingOptions> timingOptions,
            ILogger<UpdateDataBackgroundService> logger)
        {
            this.indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.cacheManager = cacheManager;
            this.logger = logger;
            minutesBetweenRun = (uint)timingOptions.Value.MinutesBetweenRun;

            Trace.Listeners.Add(new MyTraceListener(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await semaphore.WaitAsync(stoppingToken);

                    logger.LogInformation("Running background update.");

                    var updateTask = new CrawlTask(connection, client, 1);

                    await updateTask.Run(stoppingToken);

                    logger.LogInformation("Update finished. Refreshing index and clearing cache.");

                    indexManager.UpdateIndex();

                    cacheManager.Clear();

                    logger.LogInformation("Background update completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background update failed due to error.");
                }
                finally
                {
                    semaphore.Release();
                }

                await Task.Delay(TimeSpan.FromMinutes(minutesBetweenRun), stoppingToken);
            }
        }

        private class MyTraceListener : TraceListener
        {
            private readonly ILogger logger;

            public MyTraceListener(ILogger logger)
            {
                this.logger = logger;
            }

            public override void Write(string message)
            {
                logger.LogInformation(message);
            }

            public override void WriteLine(string message)
            {
                logger.LogInformation(message);
            }
        }
    }
}
