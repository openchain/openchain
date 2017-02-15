using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class OpenchainEntityFrameworkCoreStorageBuilder
    {
        public IServiceCollection Services { get; private set; }

        public OpenchainEntityFrameworkCoreStorageBuilder(IServiceCollection services)
        {
            Services = services;
        }
        
        private OpenchainEntityFrameworkCoreStorageBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);

            return this;
        }
    }
}
