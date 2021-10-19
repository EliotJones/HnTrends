using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HnTrends.Database;
using Microsoft.Data.Sqlite;

namespace HnTrends
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Serilog;
    using System.IO;
    using Serilog.Events;

    public static class Program
    {
        private static readonly Regex SqlNameRegex = new Regex(@"\.(\d+)_.+\.sql");

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(string.Empty, LogEventLevel.Information, rollOnFileSizeLimit: true,
                    rollingInterval:RollingInterval.Day)
                .CreateLogger();

            var webHost = CreateWebHostBuilder(args).Build();

            var connFac = ((IConnectionFactory)webHost.Services.GetService(typeof(IConnectionFactory)));

            using (var connection = connFac.Open())
            {
                connection.Open();
                await Migrate(connection);
            }

            await webHost.RunAsync();
        }

        private static async Task Migrate(SqliteConnection conn)
        {
            var assembly = typeof(Schema).Assembly;
            var resources = assembly.GetManifestResourceNames();

            var byVersion = new Dictionary<int, string>();

            foreach (var resource in resources)
            {
                var match = SqlNameRegex.Match(resource);
                if (!match.Success)
                {
                    continue;
                }

                var number = int.Parse(match.Groups[1].Value);

                byVersion.Add(number, resource);
            }

            var command = new SqliteCommand("SELECT id FROM version;", conn);
            var res = command.ExecuteScalar();

            var currentVersion = res is int i ? i : res is long vl ? vl : 0;

            foreach (var pair in byVersion.OrderBy(x => x.Key))
            {
                if (pair.Key <= currentVersion)
                {
                    continue;
                }

                using var stream = assembly.GetManifestResourceStream(pair.Value);
                using var streamReader = new StreamReader(stream!, Encoding.UTF8);

                var sql = await streamReader.ReadToEndAsync();

                command = new SqliteCommand(sql, conn)
                {
                    CommandTimeout = (int) TimeSpan.FromSeconds(120).TotalSeconds
                };

                await command.ExecuteNonQueryAsync();

                command = new SqliteCommand("UPDATE version SET id = @version;", conn);
                command.Parameters.AddWithValue("version", pair.Key);

                await command.ExecuteNonQueryAsync();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("http://localhost:5225")
                .UseStartup<Startup>();
        }
    }
}
