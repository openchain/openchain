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
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using OpenChain.Core;
using OpenChain.Models;
using Microsoft.AspNet.WebSockets.Server;
using Microsoft.AspNet.Cors.Core;
using OpenChain.Server;

namespace OpenChain
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {

        }

        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup Configuration
            IConfigurationSourceRoot configuration = new Configuration()
                .AddIniFile("config.ini");
            services.AddInstance<IConfiguration>(configuration);

            // Setup ASP.NET MVC
            services.AddMvc();

            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            // CORS Headers
            services.AddCors();
            CorsPolicy policy = new CorsPolicyBuilder().AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().Build();
            services.ConfigureCors(options => options.AddPolicy("Any", policy));

            // Ledger Store
            services.AddTransient<ILedgerStore>(ConfigurationParser.CreateLedgerStore);

            services.AddTransient<ILedgerQueries>(ConfigurationParser.CreateLedgerQueries);

            services.AddTransient<IRulesValidator>(ConfigurationParser.CreateRulesValidator);

            // Logger
            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            // Transaction Stream Subscriber
            services.AddSingleton<IStreamSubscriber>(ConfigurationParser.CreateStreamSubscriber);
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory, IConfiguration configuration, ILedgerStore store)
        {
            loggerfactory.AddConsole();

            app.Map("/stream", managedWebSocketsApp =>
            {
                // Comment this out to test native server implementations
                //managedWebSocketsApp.UseWebSockets(new WebSocketOptions() { ReplaceFeature = true });
                //var s = managedWebSocketsApp.ApplicationServices.GetService<IHttpUpgradeFeature>();
                managedWebSocketsApp.Use(next => new TransactionStreamMiddleware(next).Invoke);
            });

            // Configure the HTTP request pipeline.
            //app.UseStaticFiles();
            //app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            // Add MVC to the request pipeline.
            app.UseMvc();

            // Add the following route for porting Web API 2 controllers.
            // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");

            // Activate the stream subscriber
            app.ApplicationServices.GetService<IStreamSubscriber>();
        }
    }
}
