using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Openchain.EntityFrameworkCore
{
    public class OpenchainStorageDbContext : DbContext
    {
        public DbSet<Models.Record> Records { get; set; }
        public DbSet<Models.Transaction> Transactions { get; set; }
        public DbSet<Models.RecordMutation> RecordMutations { get; set; }

        public OpenchainStorageDbContext(DbContextOptions<OpenchainStorageDbContext> options) : base(options) { }

        protected OpenchainStorageDbContext() { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Models.Record>(b =>
            {
                b.HasKey(e => e.Key);
            });

            builder.Entity<Models.Transaction>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasAlternateKey(e => e.TransactionHash);
                b.HasAlternateKey(e => e.MutationHash);

                b.HasMany(e => e.RecordMutations).WithOne(e => e.Transaction).HasForeignKey(e => e.TransactionId);
            });

            builder.Entity<Models.RecordMutation>(b =>
            {
                b.HasKey(e => new { e.RecordKey, e.TransactionId });

                b.HasOne(e => e.Transaction).WithMany(e => e.RecordMutations).HasForeignKey(e => e.TransactionId);
            });
        }
    }
}
