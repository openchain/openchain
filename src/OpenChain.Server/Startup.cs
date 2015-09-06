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
using Microsoft.AspNet.Cors;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.WebSockets.Server;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using OpenChain.Ledger;
using OpenChain.Ledger.Validation;
using OpenChain.Server.Models;

namespace OpenChain.Server
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IHostingEnvironment env)
        {
            // Setup Configuration
            configuration = new ConfigurationBuilder()
                .AddJsonFile(env.MapPath("App_Data/config.json"))
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
                .AddCors()
                .AddJsonFormatters();

            // Logger
            services.AddTransient<ILogger>(ConfigurationParser.CreateLogger);

            // CORS Headers
            services.AddCors();
            CorsPolicy policy = new CorsPolicyBuilder().AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().Build();
            services.ConfigureCors(options => options.AddPolicy("Any", policy));

            // Ledger Store
            services.AddTransient<ITransactionStore>(ConfigurationParser.CreateLedgerStore);

            services.AddTransient<ILedgerQueries>(ConfigurationParser.CreateLedgerQueries);

            services.AddTransient<IAnchorBuilder>(ConfigurationParser.CreateAnchorBuilder);

            services.AddSingleton<IMutationValidator>(ConfigurationParser.CreateRulesValidator);

            services.AddTransient<TransactionValidator>(ConfigurationParser.CreateTransactionValidator);

            // Transaction Stream Subscriber
            services.AddSingleton<TransactionStreamSubscriber>(ConfigurationParser.CreateStreamSubscriber);

            // Anchoring
            services.AddSingleton<LedgerAnchorWorker>(ConfigurationParser.CreateLedgerAnchorWorker);
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory, IConfiguration configuration, ITransactionStore store)
        {
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

            // Activate singletons
            app.ApplicationServices.GetService<TransactionStreamSubscriber>();
            app.ApplicationServices.GetService<IMutationValidator>();
            app.ApplicationServices.GetService<LedgerAnchorWorker>();

            await ConfigurationParser.InitializeLedgerStore(app.ApplicationServices);
        }
    }
}
