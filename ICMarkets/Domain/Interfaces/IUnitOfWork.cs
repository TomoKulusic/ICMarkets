namespace ICMarkets.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IBlockchainRepository BlockchainRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
