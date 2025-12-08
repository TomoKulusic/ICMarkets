using ICMarkets.Domain.Entities;
using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ICMarkets.Infrastructure.Repositories;

/// <summary>
/// Blockchain repository implementing specific blockchain data queries.
/// Inherits common CRUD operations from BaseRepository.
/// Demonstrates inheritance and scalability best practices.
/// </summary>
public class BlockchainRepository : BaseRepository<BlockchainData>, IBlockchainRepository
{
    public BlockchainRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all blockchain data ordered by CreatedAt descending.
    /// Override base method to add custom ordering.
    /// </summary>
    public override async Task<IEnumerable<BlockchainData>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets blockchain data filtered by chain and optionally by network.
    /// Demonstrates flexible query building with optional parameters.
    /// </summary>
    public async Task<IEnumerable<BlockchainData>> GetByChainAsync(string chain, string? network = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(b => b.Chain.ToLower() == chain.ToLower());

        if (!string.IsNullOrWhiteSpace(network))
        {
            query = query.Where(b => b.Network.ToLower() == network.ToLower());
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the most recent blockchain data entry for a chain/network.
    /// Optimized with FirstOrDefaultAsync for single result.
    /// </summary>
    public async Task<BlockchainData?> GetLatestByChainAsync(string chain, string? network = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(b => b.Chain.ToLower() == chain.ToLower());

        if (!string.IsNullOrWhiteSpace(network))
        {
            query = query.Where(b => b.Network.ToLower() == network.ToLower());
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
