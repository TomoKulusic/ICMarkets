using ICMarkets.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ICMarkets.Infrastructure.ExternalServices;

public class BlockCypherClient : IBlockCypherClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BlockCypherClient> _logger;
    private readonly string? _apiToken;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private const string BaseUrl = "https://api.blockcypher.com/v1";

    public BlockCypherClient(HttpClient httpClient, ILogger<BlockCypherClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiToken = configuration["BlockCypher:ApiToken"];
        
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            _logger.LogWarning("BlockCypher API token not configured. Using unauthenticated requests (lower rate limits).");
        }
        
        // Configure Polly 8.x Resilience Pipeline for retry logic
        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => 
                        (int)response.StatusCode >= 500 || // Server errors
                        response.StatusCode == System.Net.HttpStatusCode.RequestTimeout),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber} after {Delay}ms. Reason: {Outcome}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string> GetBlockchainDataAsync(string chain, string network, CancellationToken cancellationToken = default)
    {
        // Build URL with optional API token to increase rate limits
        var url = $"{BaseUrl}/{chain}/{network}";
        if (!string.IsNullOrWhiteSpace(_apiToken))
        {
            url += $"?token={_apiToken}";
        }
        
        // Safe logging: only mask token if it exists
        var logUrl = string.IsNullOrWhiteSpace(_apiToken) ? url : url.Replace(_apiToken, "***");
        _logger.LogInformation("Calling BlockCypher API: {Url}", logUrl);

        try
        {
            // Execute HTTP request with retry pipeline
            var response = await _retryPipeline.ExecuteAsync(async ct =>
            {
                return await _httpClient.GetAsync(url, ct);
            }, cancellationToken);
            
            // Handle rate limiting specifically (don't retry 429)
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit exceeded for BlockCypher API. Consider adding an API token or reducing request frequency.");
                throw new HttpRequestException($"BlockCypher API rate limit exceeded. Wait a few minutes or add an API token to increase limits.");
            }
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("Successfully received response from BlockCypher API for {Chain}/{Network}", chain, network);
            
            return content;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("429"))
        {
            _logger.LogError("Rate limit hit. Free tier: 3 req/sec, 200 req/hour. With token: 10 req/sec, 500 req/hour.");
            throw new HttpRequestException("BlockCypher API rate limit exceeded. Please wait or add an API token.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Chain}/{Network}", chain, network);
            throw;
        }
    }
}
