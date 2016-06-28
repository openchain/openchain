using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Openchain.Server;

namespace Openchain
{
    public class Program
    {
        public static void Main(string[] args) =>
            new WebHostBuilder().UseKestrel().UseIISIntegration().UseStartup<Startup>().Build().Run();
    }
}
