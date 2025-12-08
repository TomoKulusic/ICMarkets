using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Contexts;
using ICMarkets.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ICMarkets.FunctionalTests;

/// <summary>
/// Custom WebApplicationFactory for functional tests.
/// Configures in-memory SQLite database and mocked external services.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private SqliteConnection? _connection;
    public Mock<IBlockCypherClient> MockBlockCypherClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<WriteDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<ReadDbContext>))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Create and open a single SQLite in-memory connection
            // Keep it open so the database persists across requests
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Configure all DbContexts to use the same in-memory connection
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));
            
            services.AddDbContext<WriteDbContext>(options =>
                options.UseSqlite(_connection));
            
            services.AddDbContext<ReadDbContext>(options =>
                options.UseSqlite(_connection));

            // Remove existing BlockCypher client and replace with mock
            var clientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlockCypherClient));
            if (clientDescriptor != null)
            {
                services.Remove(clientDescriptor);
            }

            // Mock BlockCypher API responses with realistic data
            MockBlockCypherClient
                .Setup(x => x.GetBlockchainDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string chain, string network, CancellationToken _) =>
                    $"{{\"name\":\"{chain}.{network}\",\"height\":123456,\"hash\":\"0000000000000000001\",\"time\":\"2024-01-01T00:00:00Z\"}}");

            services.AddScoped(_ => MockBlockCypherClient.Object);

            // Build the service provider and initialize the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public new void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
