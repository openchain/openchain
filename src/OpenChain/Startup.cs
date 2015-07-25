using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Cors;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using OpenChain.Models;
using Microsoft.AspNet.WebSockets.Server;
using Microsoft.AspNet.Cors.Core;
using OpenChain.Ledger;

namespace OpenChain
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IHostingEnvironment env)
        {
            // Setup Configuration
            configuration = new ConfigurationBuilder()
                .AddJsonFile($"{env.WebRootPath}/config.json")
                .Build();
        }

        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(_ => this.configuration);

            // Setup ASP.NET MVC
            services.AddMvc();

            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            // CORS Headers
            services.AddCors();
            CorsPolicy policy = new CorsPolicyBuilder().AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().Build();
            services.ConfigureCors(options => options.AddPolicy("Any", policy));

            // Ledger Store
            services.AddTransient<ITransactionStore>(ConfigurationParser.CreateLedgerStore);

            services.AddTransient<ILedgerQueries>(ConfigurationParser.CreateLedgerQueries);

            services.AddSingleton<IMutationValidator>(ConfigurationParser.CreateRulesValidator);

            services.AddSingleton<MasterProperties>(ConfigurationParser.CreateMasterProperties);

            services.AddTransient<TransactionValidator>(ConfigurationParser.CreateTransactionValidator);

            // Logger
            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            // Transaction Stream Subscriber
            services.AddSingleton<IStreamSubscriber>(ConfigurationParser.CreateStreamSubscriber);
        }

        // Configure is called after ConfigureServices is called.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory, IConfiguration configuration, ITransactionStore store)
        {
            loggerfactory.AddConsole();

            app.Map("/stream", managedWebSocketsApp =>
            {
                if (bool.Parse(configuration["enable_transaction_stream"]))
                {
                    managedWebSocketsApp.UseWebSockets(new WebSocketOptions() { ReplaceFeature = true });
                    managedWebSocketsApp.Use(next => new TransactionStreamMiddleware(next).Invoke);
                }
            });

            // Configure the HTTP request pipeline.
            //app.UseStaticFiles();
            //app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            // Add MVC to the request pipeline.
            app.UseMvc();

            // Add the following route for porting Web API 2 controllers.
            // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");

            // Activate singletons
            app.ApplicationServices.GetService<IStreamSubscriber>();
            app.ApplicationServices.GetService<IMutationValidator>();

            await ConfigurationParser.InitializeLedgerStore(app.ApplicationServices);
        }
    }
}
