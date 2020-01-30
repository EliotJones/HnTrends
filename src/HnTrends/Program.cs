namespace HnTrends
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Serilog;
    using System.IO;
    using Serilog.Events;

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(string.Empty, LogEventLevel.Information, rollOnFileSizeLimit: true,
                    rollingInterval:RollingInterval.Day)
                .CreateLogger();

            CreateWebHostBuilder(args).Build().Run();
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
