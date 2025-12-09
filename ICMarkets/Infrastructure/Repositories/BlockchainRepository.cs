using ICMarkets.Domain.Entities;
using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ICMarkets.Infrastructure.Repositories;

/// <summary>
/// Blockchain repository using Entity Framework Core for data access.
/// Uses AsNoTracking for read operations to optimize query performance.
/// </summary>
public class BlockchainRepository : IBlockchainRepository
{
    private readonly ApplicationDbContext _context;

    public BlockchainRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all blockchain data ordered by CreatedAt descending.
    /// Uses AsNoTracking for optimal query performance.
    /// </summary>
    public async Task<IEnumerable<BlockchainData>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // AsNoTracking() improves performance for read-only queries by:
        // - Not creating change tracking snapshots
        // - Reducing memory overhead
        // - Faster query execution for large result sets
        return await _context.BlockchainData
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets blockchain data filtered by chain and optionally by network.
    /// Uses case-insensitive comparison with NOCASE collation for optimal index usage.
    /// </summary>
    public async Task<IEnumerable<BlockchainData>> GetByChainAsync(
        string chain, 
        string? network = null, 
        CancellationToken cancellationToken = default)
    {
        // Using EF.Functions.Collate with NOCASE instead of .ToLower() because:
        // 1. Leverages database indexes - Column is indexed with NOCASE collation
        // 2. Server-side execution - Comparison happens in SQL, not in memory
        // 3. Better performance - No string conversion overhead
        // 4. SQLite-optimized - Uses native SQLite collation function
        // 
        // Why NOT .ToLower():
        // - b.Chain.ToLower() == chain.ToLower() would bypass indexes
        // - Forces function execution per row (slower)
        // - Cannot use composite index IX_BlockchainData_Chain_CreatedAt efficiently
        //
        // Both sides use Collate() to ensure consistent comparison behavior
        var query = _context.BlockchainData
            .AsNoTracking()
            .Where(b => EF.Functions.Collate(b.Chain, "NOCASE") == EF.Functions.Collate(chain, "NOCASE"));

        // Optional network filter - only applied if network parameter is provided
        // Allows flexible querying: GetByChainAsync("btc") or GetByChainAsync("btc", "main")
        if (!string.IsNullOrWhiteSpace(network))
        {
            query = query.Where(b => EF.Functions.Collate(b.Network, "NOCASE") == EF.Functions.Collate(network, "NOCASE"));
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the most recent blockchain data entry for a chain/network.
    /// Uses case-insensitive comparison with NOCASE collation for optimal index usage.
    /// </summary>
    public async Task<BlockchainData?> GetLatestByChainAsync(
        string chain, 
        string? network = null, 
        CancellationToken cancellationToken = default)
    {
        // Same case-insensitive collation strategy as GetByChainAsync
        // EF.Functions.Collate ensures index usage and server-side execution
        var query = _context.BlockchainData
            .AsNoTracking()
            .Where(b => EF.Functions.Collate(b.Chain, "NOCASE") == EF.Functions.Collate(chain, "NOCASE"));

        if (!string.IsNullOrWhiteSpace(network))
        {
            query = query.Where(b => EF.Functions.Collate(b.Network, "NOCASE") == EF.Functions.Collate(network, "NOCASE"));
        }

        // FirstOrDefaultAsync with OrderByDescending gets the most recent record
        // More efficient than .ToListAsync().FirstOrDefault() as it limits results in SQL
        return await query
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a single blockchain data entry to the database.
    /// Uses default change tracking for write operations.
    /// </summary>
    public async Task<BlockchainData> AddAsync(
        BlockchainData blockchainData, 
        CancellationToken cancellationToken = default)
    {
        // No AsNoTracking() for write operations - change tracking is required
        // EF Core needs to track the entity state for INSERT operations
        await _context.BlockchainData.AddAsync(blockchainData, cancellationToken);
        return blockchainData;
    }

    /// <summary>
    /// Adds multiple blockchain data entries.
    /// Uses default change tracking for write operations.
    /// </summary>
    public async Task<IEnumerable<BlockchainData>> AddRangeAsync(
        IEnumerable<BlockchainData> blockchainData, 
        CancellationToken cancellationToken = default)
    {
        // Convert to List to avoid multiple enumeration
        // AddRangeAsync is more efficient than multiple AddAsync calls for bulk inserts
        var dataList = blockchainData.ToList();
        await _context.BlockchainData.AddRangeAsync(dataList, cancellationToken);
        return dataList;
    }
}
