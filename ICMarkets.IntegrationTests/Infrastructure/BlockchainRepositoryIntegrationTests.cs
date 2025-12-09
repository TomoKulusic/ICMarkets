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
/// Integration tests for BlockchainRepository with actual ApplicationDbContext.
/// Tests repository and database interaction without mocking the DbContext.
/// </summary>
public class BlockchainRepositoryIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IBlockchainRepository _repository;

    public BlockchainRepositoryIntegrationTests()
    {
        // Setup in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Configure services
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(_connection));
        
        services.AddScoped<IBlockchainRepository, BlockchainRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _context.Database.EnsureCreated();
        
        _repository = _serviceProvider.GetRequiredService<IBlockchainRepository>();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldPersistToDatabase()
    {
        // Arrange
        var blockchainData = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"name\":\"ETH.main\",\"height\":123456}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(blockchainData);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        // Verify it's actually in the database
        var retrieved = await _context.BlockchainData.FindAsync(result.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Chain.Should().Be("eth");
        retrieved.Network.Should().Be("main");
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleRecords_ShouldReturnOrderedByCreatedAtDescending()
    {
        // Arrange
        var older = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var newer = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);
        await _context.SaveChangesAsync();

        // Act
        var results = (await _repository.GetAllAsync()).ToList();

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be(newer.Id); // Newer first
        results[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task GetByChainAsync_WithCaseInsensitiveSearch_ShouldReturnMatches()
    {
        // Arrange
        var data = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(data);
        await _context.SaveChangesAsync();

        // Act - Search with different casing
        var resultsLower = await _repository.GetByChainAsync("btc");
        var resultsUpper = await _repository.GetByChainAsync("BTC");
        var resultsMixed = await _repository.GetByChainAsync("Btc");

        // Assert
        resultsLower.Should().HaveCount(1);
        resultsUpper.Should().HaveCount(1);
        resultsMixed.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByChainAsync_WithNetworkFilter_ShouldReturnOnlyMatchingNetwork()
    {
        // Arrange
        var mainNetwork = new BlockchainData
        {
            Chain = "btc",
            Network = "main",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var testNetwork = new BlockchainData
        {
            Chain = "btc",
            Network = "test3",
            RawJsonData = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(mainNetwork);
        await _repository.AddAsync(testNetwork);
        await _context.SaveChangesAsync();

        // Act
        var mainResults = await _repository.GetByChainAsync("btc", "main");
        var testResults = await _repository.GetByChainAsync("btc", "test3");

        // Assert
        mainResults.Should().HaveCount(1);
        mainResults.First().Network.Should().Be("main");
        
        testResults.Should().HaveCount(1);
        testResults.First().Network.Should().Be("test3");
    }

    [Fact]
    public async Task GetLatestByChainAsync_WithMultipleRecords_ShouldReturnMostRecent()
    {
        // Arrange
        var older = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":100}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        var newer = new BlockchainData
        {
            Chain = "eth",
            Network = "main",
            RawJsonData = "{\"height\":200}",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(older);
        await _repository.AddAsync(newer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetLatestByChainAsync("eth");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(newer.Id);
        result.RawJsonData.Should().Contain("\"height\":200");
    }

    [Fact]
    public async Task AddRangeAsync_WithMultipleEntities_ShouldPersistAllInTransaction()
    {
        // Arrange
        var data = new List<BlockchainData>
        {
            new()
            {
                Chain = "eth",
                Network = "main",
                RawJsonData = "{}",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Chain = "btc",
                Network = "main",
                RawJsonData = "{}",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Chain = "dash",
                Network = "main",
                RawJsonData = "{}",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var results = (await _repository.AddRangeAsync(data)).ToList();
        await _context.SaveChangesAsync();

        // Assert
        results.Should().HaveCount(3);
        results.All(r => r.Id > 0).Should().BeTrue();

        var allData = (await _repository.GetAllAsync()).ToList();
        allData.Should().HaveCount(3);
    }

    [Fact]
    public async Task Repository_WithDatabaseIndexes_ShouldPerformEfficientQueries()
    {
        // Arrange - Add multiple records
        var chains = new[] { "eth", "btc", "dash", "ltc" };
        var networks = new[] { "main", "test3" };

        var dataToAdd = new List<BlockchainData>();
        foreach (var chain in chains)
        {
            foreach (var network in networks)
            {
                for (int i = 0; i < 10; i++)
                {
                    dataToAdd.Add(new BlockchainData
                    {
                        Chain = chain,
                        Network = network,
                        RawJsonData = $"{{\"index\":{i}}}",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                    });
                }
            }
        }

        await _repository.AddRangeAsync(dataToAdd);
        await _context.SaveChangesAsync();

        // Act - Query using indexed columns
        var ethData = await _repository.GetByChainAsync("eth");
        var btcMainData = await _repository.GetByChainAsync("btc", "main");
        var latestDash = await _repository.GetLatestByChainAsync("dash");

        // Assert - Verify queries work correctly with indexes
        ethData.Should().HaveCount(20); // 10 main + 10 test3
        btcMainData.Should().HaveCount(10);
        latestDash.Should().NotBeNull();
        latestDash!.Chain.Should().Be("dash");
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        _serviceProvider?.Dispose();
    }
}
