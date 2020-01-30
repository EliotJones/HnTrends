using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
namespace HnTrends
{
    using Caches;
    using Indexer;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Services;
    using System.Data.SQLite;

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
            services.AddMemoryCache();

            services.AddSingleton(x =>
            {
                var str = x.GetService<IOptions<FileLocations>>().Value.Database;
                var conn = new SQLiteConnection($"Data Source={str}");
                return conn.OpenAndReturn();
            });

            services.AddSingleton<IIndexManager>(x =>
            {
                var index = x.GetService<IOptions<FileLocations>>().Value.Index;
                return new IndexManager(index, x.GetService<SQLiteConnection>());
            });

            services.AddSingleton<IPostCountsCache, PostCountsCache>();
            services.AddSingleton<IStoryCountCache, StoryCountCache>();
            services.AddSingleton<ITrendService, TrendService>();

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
