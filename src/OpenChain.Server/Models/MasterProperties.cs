using Microsoft.Framework.Configuration;

namespace OpenChain.Server.Models
{
    public class MasterProperties
    {
        private readonly IConfiguration configuration;

        public MasterProperties(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Name => this.configuration["name"];

        public string Tos => this.configuration["tos"];
    }
}
