namespace HnTrends.Services
{
    using Core;
    using Database;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class UpdateScoresBackgroundService : BackgroundService
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly HttpClient client;
        private readonly ILogger<UpdateScoresBackgroundService> logger;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public UpdateScoresBackgroundService(
            IConnectionFactory connectionFactory,
            HttpClient client,
            ILogger<UpdateScoresBackgroundService> logger)
        {
            this.connectionFactory = connectionFactory;
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.logger = logger;

            Trace.Listeners.Add(new MyTraceListener(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var random = new Random();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    using var connection = connectionFactory.Open();

                    await semaphore.WaitAsync(stoppingToken);

                    logger.LogInformation("Running background update of scores.");

                    var ids = await GetEntriesToUpdate(stoppingToken, connection);

                    logger.LogInformation($"Found {ids.Count} stories to update the score for.");

                    var scoresById = new Dictionary<int, int>();

                    var processedCount = 0;

                    foreach (var id in ids)
                    {
                        try
                        {
                            var response = await client
                                .GetStringAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json");

                            if (string.IsNullOrWhiteSpace(response))
                            {
                                continue;
                            }

                            var item = JsonConvert.DeserializeObject<HnStoryPartial>(response);

                            scoresById[id] = item.Score;

                            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(2, 30)), stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Failed getting item with id: {id}.");
                            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        }

                        if (scoresById.Count > 50)
                        {
                            var sql = "UPDATE story SET score = @score WHERE id = @id;";
                            await using var transaction = connection.BeginTransaction();

                            var command = new SqliteCommand(sql, connection, transaction);

                            foreach (var pair in scoresById)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("score", pair.Value);
                                command.Parameters.AddWithValue("id", pair.Key);

                                await command.ExecuteNonQueryAsync(stoppingToken);
                            }

                            transaction.Commit();
                        }

                        processedCount++;

                        if (processedCount % 1000 == 0)
                        {
                            logger.LogInformation($"{ids.Count - processedCount} scores remaining to sync.");
                        }
                    }

                    logger.LogInformation("Background update of scores completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background update of scores failed due to error.");
                }
                finally
                {
                    semaphore.Release();
                }

                await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
            }
        }

        private async Task<IReadOnlyList<int>> GetEntriesToUpdate(CancellationToken token, SqliteConnection connection)
        {
            // 3 weeks ago.
            var bound = DateTime.UtcNow.AddDays(-3 * 7);

            var query = new SqliteCommand("SELECT id FROM story WHERE score IS NULL OR time >= @minTime;", connection);

            query.Parameters.AddWithValue("minTime", Entry.DateToTime(bound));

            var results = new List<int>();

            await using var reader = await query.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                results.Add(reader.GetInt32(0));
            }

            return results;
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

        private struct EntryToUpdate
        {
            public int Id { get; set; }
        }

        private class HnStoryPartial
        {
            public int Id { get; set; }

            public int Score { get; set; }
        }
    }
}