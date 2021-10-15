namespace HnTrends
{
    using Caches;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Services;
    using System;
    using System.Net.Http;


    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<FileLocations>(Configuration.GetSection("FileLocations"));
            services.Configure<TimingOptions>(Configuration.GetSection("Timing"));

            services.AddMemoryCache();

            services.AddSingleton(x => new HttpClient
            {
                BaseAddress = new Uri("https://news.ycombinator.com/"),
                Timeout = TimeSpan.FromMinutes(1)
            });

            services.AddSingleton(x =>
            {
                var str = x.GetService<IOptions<FileLocations>>().Value.Database;
                var conn = new SqliteConnection($"Data Source={str}");
                conn.Open();
                return conn;
            });

            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IPostCountsCache, PostCountsCache>();
            services.AddSingleton<IStoryCountCache, StoryCountCache>();
            services.AddSingleton<IResultsCache, ResultsCache>();
            services.AddSingleton<ITrendService, TrendService>();

            services.AddHostedService<UpdateDataBackgroundService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
