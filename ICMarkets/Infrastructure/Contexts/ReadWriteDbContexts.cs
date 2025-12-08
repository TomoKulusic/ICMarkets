using ICMarkets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ICMarkets.Infrastructure.Contexts;

/// <summary>
/// Read-only database context for query operations.
/// Points to read replica for scalability.
/// Optimized for read performance with no change tracking.
/// </summary>
public class ReadDbContext : ApplicationDbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) 
        : base(ConvertOptions(options))
    {
        // Read-only: No change tracking for better performance
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    // Helper method to convert DbContextOptions<ReadDbContext> to DbContextOptions<ApplicationDbContext>
    private static DbContextOptions<ApplicationDbContext> ConvertOptions(DbContextOptions<ReadDbContext> options)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Copy all options from the derived context to the base context
        foreach (var extension in options.Extensions)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
        }
        
        return builder.Options;
    }

    // Override SaveChanges to prevent writes
    public override int SaveChanges()
    {
        throw new InvalidOperationException("This context is read-only. Use WriteDbContext for write operations.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new InvalidOperationException("This context is read-only. Use WriteDbContext for write operations.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only. Use WriteDbContext for write operations.");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only. Use WriteDbContext for write operations.");
    }
}

/// <summary>
/// Write database context for command operations.
/// Points to primary database for writes.
/// </summary>
public class WriteDbContext : ApplicationDbContext
{
    public WriteDbContext(DbContextOptions<WriteDbContext> options) 
        : base(ConvertOptions(options))
    {
    }

    // Helper method to convert DbContextOptions<WriteDbContext> to DbContextOptions<ApplicationDbContext>
    private static DbContextOptions<ApplicationDbContext> ConvertOptions(DbContextOptions<WriteDbContext> options)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Copy all options from the derived context to the base context
        foreach (var extension in options.Extensions)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
        }
        
        return builder.Options;
    }
}
