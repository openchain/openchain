using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class OpenchainAnchorStateDbContext : DbContext
    {
        public DbSet<Models.Anchor> Anchors { get; set; }

        public OpenchainAnchorStateDbContext(DbContextOptions<OpenchainAnchorStateDbContext> options) : base(options) { }

        protected OpenchainAnchorStateDbContext() { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Models.Anchor>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasAlternateKey(e => e.Position);
            });
        }
    }
}
