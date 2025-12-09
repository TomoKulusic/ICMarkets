using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;

namespace ICMarkets.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation managing transaction scope and coordinating saves.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IBlockchainRepository? _blockchainRepository;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IBlockchainRepository BlockchainRepository => 
        _blockchainRepository ??= new BlockchainRepository(_context);

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// Returns the number of state entries written to the database.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context?.Dispose();
        }
        _disposed = true;
    }
}
