using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;

namespace ICMarkets.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IBlockchainRepository? _blockchainRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IBlockchainRepository BlockchainRepository
    {
        get
        {
            _blockchainRepository ??= new BlockchainRepository(_context);
            return _blockchainRepository;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
