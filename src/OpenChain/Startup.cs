using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.WebSockets.Server;
using System.Text;
using System.Threading;

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
            services.AddMvc();
            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Configure the HTTP request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc();
            // Add the following route for porting Web API 2 controllers.
            // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            
            app.UseWebSockets(new WebSocketOptions() { ReplaceFeature = true });

            //app.Map("/Managed", managedWebSocketsApp =>
            //{
            //    // Comment this out to test native server implementations
            //    managedWebSocketsApp.UseWebSockets(new WebSocketOptions()
            //    {
            //        ReplaceFeature = true,
            //    });

            //    managedWebSocketsApp.Use(async (context, next) =>
            //    {
            //        //if (context.IsWebSocketRequest)
            //        {
            //            Console.WriteLine("Echo: " + context.Request.Path);
            //            var webSocket = await context.AcceptWebSocketAsync();
            //            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("\"Hello World\"")), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            //            return;
            //        }
            //        await next();
            //    });
            //});
        }
    }
}
