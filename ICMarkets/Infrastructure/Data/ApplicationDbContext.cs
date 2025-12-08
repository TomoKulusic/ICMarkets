using ICMarkets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ICMarkets.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<BlockchainData> BlockchainData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BlockchainData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Chain).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Network).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RawJsonData).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.Chain, e.CreatedAt });
        });
    }
}
