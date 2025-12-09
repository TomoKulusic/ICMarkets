using FluentAssertions;
using ICMarkets.Domain.Entities;
using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using ICMarkets.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ICMarkets.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for Unit of Work pattern implementation.
/// Tests transaction coordination and SaveChanges behavior with actual database.
/// </summary>
public class UnitOfWorkIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkIntegrationTests()
    {
        // Setup in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Configure services
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(_connection));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBlockchainRepository, BlockchainRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _context.Database.EnsureCreated();
        
        _unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithSingleAdd_ShouldCommitToDatabase()
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"test\":true}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _unitOfWork.BlockchainRepository.AddAsync(data);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(1); // 1 entity saved
        data.Id.Should().BeGreaterThan(0);

        // Verify persistence
        var retrieved = await _context.BlockchainData.FindAsync(data.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleAdds_ShouldCommitAllInTransaction()
    {
        // Arrange
        var data1 = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var data2 = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var data3 = new BlockchainData
        {
            Chain = "dash",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _unitOfWork.BlockchainRepository.AddAsync(data1);
        await _unitOfWork.BlockchainRepository.AddAsync(data2);
        await _unitOfWork.BlockchainRepository.AddAsync(data3);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(3); // 3 entities saved
        
        var allData = await _unitOfWork.BlockchainRepository.GetAllAsync();
        allData.Should().HaveCount(3);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_ShouldReturnZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(0); // No changes
    }

    [Fact]
    public async Task UnitOfWork_MultipleOperations_ShouldShareSameDbContext()
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Add and query using same UnitOfWork
        await _unitOfWork.BlockchainRepository.AddAsync(data);
        await _unitOfWork.SaveChangesAsync();

        var retrieved = await _unitOfWork.BlockchainRepository.GetLatestByChainAsync("btc");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(data.Id);
    }

    [Fact]
    public async Task UnitOfWork_Dispose_ShouldNotAffectPreviouslySavedData()
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "ltc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        int savedId;

        // Act - Save and dispose
        using (var scope = _serviceProvider.CreateScope())
        {
            var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await scopedUnitOfWork.BlockchainRepository.AddAsync(data);
            await scopedUnitOfWork.SaveChangesAsync();
            savedId = data.Id;
        }

        // Assert - Data should still be in database after dispose
        var retrieved = await _context.BlockchainData.FindAsync(savedId);
        retrieved.Should().NotBeNull();
        retrieved!.Chain.Should().Be("ltc");
    }

    [Fact]
    public async Task UnitOfWork_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "dash",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var cts = new CancellationTokenSource();
        
        // Act
        await _unitOfWork.BlockchainRepository.AddAsync(data, cts.Token);
        cts.Cancel();

        // Assert - Should handle cancellation gracefully
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cts.Token);
        });
    }

    [Fact]
    public async Task BlockchainRepository_ThroughUnitOfWork_ShouldAccessSameRepository()
    {
        // Arrange & Act
        var repo1 = _unitOfWork.BlockchainRepository;
        var repo2 = _unitOfWork.BlockchainRepository;

        // Assert - Should return same instance (lazy initialization)
        repo1.Should().BeSameAs(repo2);
    }

    [Fact]
    public async Task UnitOfWork_BatchOperations_ShouldCommitAtomically()
    {
        // Arrange
        var dataList = Enumerable.Range(1, 50).Select(i => new BlockchainData
        {
            Chain = i % 2 == 0 ? "eth" : "btc",
            Network = "main",
            RawJsonData = $"{{\"batch\":{i}}}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-i)
        }).ToList();

        // Act - Add all in batch
        await _unitOfWork.BlockchainRepository.AddRangeAsync(dataList);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(50);
        
        var allData = await _unitOfWork.BlockchainRepository.GetAllAsync();
        allData.Should().HaveCount(50);
        
        var ethData = await _unitOfWork.BlockchainRepository.GetByChainAsync("eth");
        ethData.Should().HaveCount(25);
        
        var btcData = await _unitOfWork.BlockchainRepository.GetByChainAsync("btc");
        btcData.Should().HaveCount(25);
    }

    public void Dispose()
    {
        try
        {
            _context?.Database.EnsureDeleted();
        }
        catch
        {
            // Context may already be disposed by UnitOfWork
        }
        finally
        {
            _unitOfWork?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
