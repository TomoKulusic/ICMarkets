using ICMarkets.Domain.Interfaces;
using ICMarkets.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ICMarkets.FunctionalTests;

/// <summary>
/// Custom WebApplicationFactory for functional tests with Entity Framework Core.
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
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            // Create and open a single SQLite in-memory connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add test DbContext configuration with in-memory SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Remove existing BlockCypher client and replace with mock
            var clientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlockCypherClient));
            if (clientDescriptor != null)
            {
                services.Remove(clientDescriptor);
            }

            // Mock BlockCypher API responses
            MockBlockCypherClient
                .Setup(x => x.GetBlockchainDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string chain, string network, CancellationToken _) =>
                    $"{{\"name\":\"{chain}.{network}\",\"height\":123456,\"hash\":\"0000000000000000001\",\"time\":\"2024-01-01T00:00:00Z\"}}");

            services.AddScoped(_ => MockBlockCypherClient.Object);

            // Initialize database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
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
