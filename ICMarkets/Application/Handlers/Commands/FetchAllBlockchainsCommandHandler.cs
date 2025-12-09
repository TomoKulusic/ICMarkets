using ICMarkets.Application.Commands;
using ICMarkets.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ICMarkets.Application.Handlers.Commands;

public class FetchAllBlockchainsCommandHandler : IRequestHandler<FetchAllBlockchainsCommand, IEnumerable<BlockchainDataDto>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FetchAllBlockchainsCommandHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly int _maxConcurrentRequests;
    private readonly int _delayBetweenRequestsMs;

    private static readonly List<(string Chain, string Network)> BlockchainEndpoints = new()
    {
        ("eth", "main"),
        ("dash", "main"),
        ("btc", "main"),
        ("btc", "test3"),
        ("ltc", "main")
    };

    public FetchAllBlockchainsCommandHandler(
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<FetchAllBlockchainsCommandHandler> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _configuration = configuration;
        
        // Check if API token is configured to determine rate limits
        var hasApiToken = !string.IsNullOrWhiteSpace(_configuration["BlockCypher:ApiToken"]);
        
        // With API token: 10 req/sec (use 5 concurrent to be safe)
        // Without token: 3 req/sec (use 2 concurrent to be safe)
        _maxConcurrentRequests = hasApiToken ? 5 : 2;
        _delayBetweenRequestsMs = hasApiToken ? 100 : 350; // Throttle between requests
        
        _rateLimitSemaphore = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);
        
        _logger.LogInformation(
            "FetchAll configured with {MaxConcurrent} concurrent requests and {Delay}ms delay (API token: {HasToken})",
            _maxConcurrentRequests, _delayBetweenRequestsMs, hasApiToken);
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(FetchAllBlockchainsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching data for all {Count} blockchains in parallel with rate limiting ({MaxConcurrent} concurrent requests)",
            BlockchainEndpoints.Count, _maxConcurrentRequests);

        // Create tasks for all endpoints
        var tasks = BlockchainEndpoints.Select(endpoint => 
            FetchWithRateLimitAsync(endpoint.Chain, endpoint.Network, cancellationToken)
        );

        // Execute all tasks in parallel with rate limiting
        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r != null);
        _logger.LogInformation(
            "Successfully fetched data for {SuccessCount}/{Total} blockchains in parallel",
            successCount, BlockchainEndpoints.Count);

        // Filter out null results from failed requests
        return results.Where(r => r != null).Cast<BlockchainDataDto>();
    }

    private async Task<BlockchainDataDto?> FetchWithRateLimitAsync(
        string chain, 
        string network, 
        CancellationToken cancellationToken)
    {
        // Wait for semaphore (limits concurrent requests)
        await _rateLimitSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            _logger.LogInformation("Fetching {Chain}/{Network}...", chain, network);

            var result = await FetchBlockchainDataAsync(chain, network, cancellationToken);
            
            _logger.LogInformation("Successfully fetched {Chain}/{Network}", chain, network);
            
            // Add delay to throttle request rate
            if (_delayBetweenRequestsMs > 0)
            {
                await Task.Delay(_delayBetweenRequestsMs, cancellationToken);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Chain}/{Network}", chain, network);
            return null; // Return null for failed requests, continue with others
        }
        finally
        {
            // Release semaphore to allow next request
            _rateLimitSemaphore.Release();
        }
    }

    private async Task<BlockchainDataDto> FetchBlockchainDataAsync(string chain, string network, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new FetchBlockchainDataCommand(chain, network), cancellationToken);
    }
}
