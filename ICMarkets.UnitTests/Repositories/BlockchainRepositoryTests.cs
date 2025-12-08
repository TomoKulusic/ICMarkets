using FluentAssertions;
using ICMarkets.Domain.Entities;
using ICMarkets.Infrastructure.Contexts;
using ICMarkets.Infrastructure.Data;
using ICMarkets.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ICMarkets.UnitTests.Repositories;

/// <summary>
/// Unit tests for BlockchainRepository.
/// Tests repository methods in isolation with in-memory database.
/// </summary>
public class BlockchainRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BlockchainRepository _repository;

    public BlockchainRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new BlockchainRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleEntries_ShouldReturnOrderedByCreatedAtDescending()
    {
        // Arrange
        var olderData = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{\"height\":1}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var newerData = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":2}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.BlockchainData.AddRangeAsync(olderData, newerData);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
        result[0].Chain.Should().Be("eth"); // Newer entry first
    }

    [Fact]
    public async Task GetByChainAsync_WithMatchingChain_ShouldReturnOnlyMatchingEntries()
    {
        // Arrange
        var btcData = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{\"height\":1}",
            CreatedAt = DateTime.UtcNow
        };

        var ethData = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":2}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.BlockchainData.AddRangeAsync(btcData, ethData);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByChainAsync("btc")).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Chain.Should().Be("btc");
    }

    [Fact]
    public async Task GetByChainAsync_WithNetworkFilter_ShouldReturnOnlyMatchingNetworkEntries()
    {
        // Arrange
        var btcMain = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{\"height\":1}",
            CreatedAt = DateTime.UtcNow
        };

        var btcTest = new BlockchainData
        {
            Chain = "btc",
            Network = "test3",
            RawJsonData = "{\"height\":2}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.BlockchainData.AddRangeAsync(btcMain, btcTest);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByChainAsync("btc", "main")).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Network.Should().Be("main");
    }

    [Fact]
    public async Task GetLatestByChainAsync_WithMultipleEntries_ShouldReturnMostRecent()
    {
        // Arrange
        var olderData = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":1}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var newerData = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":2}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.BlockchainData.AddRangeAsync(olderData, newerData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestByChainAsync("eth");

        // Assert
        result.Should().NotBeNull();
        result!.RawJsonData.Should().Contain("\"height\":2");
    }

    [Fact]
    public async Task AddAsync_ValidEntity_ShouldAddToDatabase()
    {
        // Arrange
        var newData = new BlockchainData
        {
            Chain = "ltc",
            Network = "main",
            RawJsonData = "{\"height\":100}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(newData);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.BlockchainData.FirstOrDefaultAsync(x => x.Chain == "ltc");
        result.Should().NotBeNull();
        result!.Chain.Should().Be("ltc");
    }

    [Theory]
    [InlineData("btc")]
    [InlineData("BTC")]
    [InlineData("Btc")]
    public async Task GetByChainAsync_CaseInsensitive_ShouldReturnResults(string chainQuery)
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{\"height\":1}",
            CreatedAt = DateTime.UtcNow
        };

        await _context.BlockchainData.AddAsync(data);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByChainAsync(chainQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
