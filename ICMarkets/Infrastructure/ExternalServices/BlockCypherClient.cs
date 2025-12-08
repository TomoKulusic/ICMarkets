using ICMarkets.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace ICMarkets.Infrastructure.ExternalServices;

public class BlockCypherClient : IBlockCypherClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BlockCypherClient> _logger;
    private const string BaseUrl = "https://api.blockcypher.com/v1";

    public BlockCypherClient(HttpClient httpClient, ILogger<BlockCypherClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetBlockchainDataAsync(string chain, string network, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{chain}/{network}";
        
        _logger.LogInformation("Calling BlockCypher API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("Successfully received response from BlockCypher API for {Chain}/{Network}", chain, network);
            
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Chain}/{Network}", chain, network);
            throw;
        }
    }

    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s delay due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }
}
