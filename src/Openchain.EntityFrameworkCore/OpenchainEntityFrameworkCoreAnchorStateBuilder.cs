using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class OpenchainEntityFrameworkCoreAnchorStateBuilder
    {
        public IServiceCollection Services { get; private set; }

        public OpenchainEntityFrameworkCoreAnchorStateBuilder(IServiceCollection services)
        {
            Services = services;
        }
        
        private OpenchainEntityFrameworkCoreAnchorStateBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);

            return this;
        }
    }
}
