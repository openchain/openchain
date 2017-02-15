using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Openchain.Infrastructure;

namespace Openchain.EntityFrameworkCore
{
    public static class OpenchainEntityFrameworkCoreServiceCollectionExtension
    {
        public static OpenchainEntityFrameworkCoreStorageBuilder AddEntityFrameworkCoreStorage<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            services.AddScoped(typeof(IStorageEngine), typeof(EntityFrameworkCoreLedger<>).MakeGenericType(typeof(TContext)));
            services.AddScoped(typeof(ILedgerQueries), typeof(EntityFrameworkCoreLedger<>).MakeGenericType(typeof(TContext)));
            services.AddScoped(typeof(ILedgerIndexes), typeof(EntityFrameworkCoreLedger<>).MakeGenericType(typeof(TContext)));

            return new OpenchainEntityFrameworkCoreStorageBuilder(services);
        }

        public static OpenchainEntityFrameworkCoreAnchorStateBuilder AddEntityFrameworkCoreAnchorState<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            services.AddScoped(typeof(IAnchorState), typeof(EntityFrameworkCoreAnchorState<>).MakeGenericType(typeof(TContext)));

            return new OpenchainEntityFrameworkCoreAnchorStateBuilder(services);
        }
    }
}
