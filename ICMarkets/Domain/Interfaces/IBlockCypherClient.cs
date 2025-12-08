namespace ICMarkets.Domain.Interfaces;

public interface IBlockCypherClient
{
    Task<string> GetBlockchainDataAsync(string chain, string network, CancellationToken cancellationToken = default);
}
