using Microsoft.EntityFrameworkCore;
using Openchain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class EntityFrameworkCoreAnchorState<TContext> : IAnchorState where TContext : DbContext
    {
        public TContext Context { get; private set; }

        public DbSet<Models.Anchor> Anchors { get { return Context.Set<Models.Anchor>(); } }

        public EntityFrameworkCoreAnchorState(TContext context)
        {
            Context = context;
        }

        public Task Initialize()
        {
            return Task.FromResult(0);
        }

        public async Task CommitAnchor(LedgerAnchor anchor)
        {
            var newAnchor = new Models.Anchor
            {
                Position = anchor.Position.ToByteArray(),
                FullLedgerHash = anchor.FullStoreHash.ToByteArray(),
                TransactionCount = anchor.TransactionCount
            };

            Anchors.Add(newAnchor);

            await Context.SaveChangesAsync();
        }

        public async Task<LedgerAnchor> GetLastAnchor()
        {
            var anchor = await Anchors.OrderBy(a => a.Id).Take(1).FirstOrDefaultAsync();
            
            var ledgerAnchor = new LedgerAnchor(new ByteString(anchor.Position), new ByteString(anchor.FullLedgerHash), anchor.TransactionCount);

            return ledgerAnchor;
        }
    }
}
