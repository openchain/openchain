// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.WebSockets.Server;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Openchain.Ledger;
using Openchain.Ledger.Validation;
using Openchain.Server.Models;

namespace Openchain.Server
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IHostingEnvironment env, IApplicationEnvironment application)
        {
            // Setup Configuration
            configuration = new ConfigurationBuilder()
                .AddJsonFile(env.MapPath("App_Data/config.json"))
                .AddUserSecrets("Openchain.Server")
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// Adds services to the dependency injection container.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.BuildServiceProvider().GetService<ILoggerFactory>().AddConsole();

            services.AddSingleton<IConfiguration>(_ => this.configuration);

            // Setup ASP.NET MVC
            services
                .AddMvcCore()
                .AddViews()
                .AddJsonFormatters();

            // Logger
            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            LogStartup(services.BuildServiceProvider().GetService<ILogger>(), services.BuildServiceProvider().GetService<IApplicationEnvironment>());

            // CORS Headers
            services.AddCors();

            // Ledger Store
            services.AddTransient<IStorageEngine>(ConfigurationParser.CreateLedgerStore);

            services.AddTransient<ILedgerQueries>(ConfigurationParser.CreateLedgerQueries);

            services.AddTransient<IAnchorBuilder>(ConfigurationParser.CreateAnchorBuilder);

            services.AddSingleton<IMutationValidator>(ConfigurationParser.CreateRulesValidator);

            services.AddTransient<TransactionValidator>(ConfigurationParser.CreateTransactionValidator);

            // Transaction Stream Subscriber
            services.AddSingleton<TransactionStreamSubscriber>(ConfigurationParser.CreateStreamSubscriber);

            // Anchoring
            services.AddSingleton<LedgerAnchorWorker>(ConfigurationParser.CreateLedgerAnchorWorker);
        }

        private static void LogStartup(ILogger logger, IApplicationEnvironment environment)
        {
            logger.LogInformation($"Starting Openchain v{environment.ApplicationVersion} ({environment.RuntimeFramework.FullName})");
            logger.LogInformation(" ");
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory, IConfiguration configuration, IStorageEngine store)
        {
            app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

            app.Map("/stream", managedWebSocketsApp =>
            {
                if (bool.Parse(configuration["enable_transaction_stream"]))
                {
                    managedWebSocketsApp.UseWebSockets(new WebSocketOptions() { ReplaceFeature = true });
                    managedWebSocketsApp.Use(next => new TransactionStreamMiddleware(next).Invoke);
                }
            });

            // Add MVC to the request pipeline.
            app.UseMvc();

            await ConfigurationParser.InitializeLedgerStore(app.ApplicationServices);

            // Activate singletons
            app.ApplicationServices.GetService<TransactionStreamSubscriber>();
            app.ApplicationServices.GetService<IMutationValidator>();
            app.ApplicationServices.GetService<LedgerAnchorWorker>();
        }
    }
}
