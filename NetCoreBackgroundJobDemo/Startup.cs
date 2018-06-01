using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopOnContainers.BuildingBlocks.Resilience.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCoreBackgroundJobDemo.Code;
using NetCoreBackgroundJobDemo.Code.Scheduling;
using NetCoreBackgroundJobDemo.Infastructure;
using System;

namespace NetCoreBackgroundJobDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            var builder = new ConfigurationBuilder();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Program>();
            }

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddMemoryCache();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add scheduled tasks & scheduler
            services.AddSingleton<IScheduledTask, PostPaymentTask>();

            services.AddScheduler((sender, args) =>
            {
                Console.Write(args.Exception.Message);
                args.SetObserved();
            });

            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));

            services.AddSingleton<IResilientHttpClientFactory, ResilientHttpClientFactory>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ResilientHttpClient>>();
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

                var retryCount = 6;
                if (!string.IsNullOrEmpty(Configuration["HttpClientRetryCount"]))
                    retryCount = int.Parse(Configuration["HttpClientRetryCount"]);

                var exceptionsAllowedBeforeBreaking = 5;
                if (!string.IsNullOrEmpty(Configuration["HttpClientExceptionsAllowedBeforeBreaking"]))
                    exceptionsAllowedBeforeBreaking = int.Parse(Configuration["HttpClientExceptionsAllowedBeforeBreaking"]);

                return new ResilientHttpClientFactory(logger, httpContextAccessor, exceptionsAllowedBeforeBreaking, retryCount);
            });
            services.AddSingleton<IHttpClient, ResilientHttpClient>(sp => sp.GetService<IResilientHttpClientFactory>().CreateResilientHttpClient());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}