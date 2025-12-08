using ICMarkets.Domain.Entities;

namespace ICMarkets.Domain.Interfaces;

public interface IBlockchainRepository
{
    Task<IEnumerable<BlockchainData>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BlockchainData>> GetByChainAsync(string chain, string? network = null, CancellationToken cancellationToken = default);
    Task<BlockchainData?> GetLatestByChainAsync(string chain, string? network = null, CancellationToken cancellationToken = default);
    Task<BlockchainData> AddAsync(BlockchainData blockchainData, CancellationToken cancellationToken = default);
    Task<IEnumerable<BlockchainData>> AddRangeAsync(IEnumerable<BlockchainData> blockchainData, CancellationToken cancellationToken = default);
}
