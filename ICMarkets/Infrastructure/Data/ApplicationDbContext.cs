using ICMarkets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ICMarkets.Infrastructure.Data;

/// <summary>
/// Application DbContext for Entity Framework Core.
/// Configured with optimized indexes and case-insensitive collation.
/// </summary>
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
            
            entity.Property(e => e.Chain)
                .IsRequired()
                .HasMaxLength(10)
                .UseCollation("NOCASE");
            
            entity.Property(e => e.Network)
                .IsRequired()
                .HasMaxLength(20)
                .UseCollation("NOCASE");
            
            entity.Property(e => e.RawJsonData)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            // Composite index for Chain + CreatedAt queries
            entity.HasIndex(e => new { e.Chain, e.CreatedAt })
                .HasDatabaseName("IX_BlockchainData_Chain_CreatedAt");
            
            // Composite index for Chain + Network + CreatedAt queries
            entity.HasIndex(e => new { e.Chain, e.Network, e.CreatedAt })
                .HasDatabaseName("IX_BlockchainData_Chain_Network_CreatedAt");
        });
    }
}
