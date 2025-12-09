using ICMarkets.Application.Commands;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ICMarkets.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Fixed)]
public class BlockchainController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BlockchainController> _logger;

    public BlockchainController(IMediator mediator, ILogger<BlockchainController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all stored blockchain data entries ordered by CreatedAt descending.
    /// Returns the complete history of all fetched blockchain data.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new string[] { })]
    [ProducesResponseType(typeof(IEnumerable<BlockchainDataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BlockchainDataDto>>> GetAllStoredBlockchainData(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all stored blockchain data from database");
        var result = await _mediator.Send(new GetAllBlockchainDataQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets stored blockchain data for a specific chain (eth, btc, dash, ltc) ordered by CreatedAt descending.
    /// Optionally filter by network (main, test3, etc.).
    /// Returns historical data from the database.
    /// </summary>
    /// <param name="chain">Blockchain type (eth, btc, dash, ltc)</param>
    /// <param name="network">Optional network filter (main, test3, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{chain}")]
    [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "chain", "network" })]
    [ProducesResponseType(typeof(IEnumerable<BlockchainDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BlockchainDataDto>>> GetStoredBlockchainDataByChain(
        string chain, 
        [FromQuery] string? network = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stored blockchain data from database for chain: {Chain}, network: {Network}", chain, network ?? "all");
        var result = await _mediator.Send(new GetBlockchainDataByChainQuery(chain, network), cancellationToken);
        
        if (!result.Any())
        {
            var message = string.IsNullOrWhiteSpace(network) 
                ? $"No stored data found for chain: {chain}" 
                : $"No stored data found for chain: {chain}, network: {network}";
            return NotFound(message);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Gets the latest stored blockchain data entry for a specific chain from the database.
    /// Optionally filter by network (main, test3, etc.).
    /// Returns the most recent entry by CreatedAt timestamp.
    /// </summary>
    /// <param name="chain">Blockchain type (eth, btc, dash, ltc)</param>
    /// <param name="network">Optional network filter (main, test3, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{chain}/latest")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "chain", "network" })]
    [ProducesResponseType(typeof(BlockchainDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlockchainDataDto>> GetLatestStoredBlockchainData(
        string chain, 
        [FromQuery] string? network = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting latest stored blockchain data from database for chain: {Chain}, network: {Network}", chain, network ?? "all");
        var result = await _mediator.Send(new GetLatestBlockchainDataQuery(chain, network), cancellationToken);
        
        if (result == null)
        {
            var message = string.IsNullOrWhiteSpace(network) 
                ? $"No stored data found for chain: {chain}" 
                : $"No stored data found for chain: {chain}, network: {network}";
            return NotFound(message);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Fetches fresh data from BlockCypher API for a specific chain and network.
    /// Stores the complete JSON response with a CreatedAt timestamp in the database.
    /// Rate limited to 10 requests per minute.
    /// </summary>
    /// <param name="chain">Blockchain type (eth, btc, dash, ltc)</param>
    /// <param name="network">Network type (main, test3, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("fetch")]
    [EnableRateLimiting(RateLimitPolicies.Fetch)]
    [ProducesResponseType(typeof(BlockchainDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BlockchainDataDto>> FetchFreshBlockchainDataFromApi(
        [FromQuery] string chain, 
        [FromQuery] string network, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chain) || string.IsNullOrWhiteSpace(network))
        {
            return BadRequest("Chain and network parameters are required");
        }

        _logger.LogInformation("Fetching fresh blockchain data from BlockCypher API for {Chain}/{Network}", chain, network);
        
        try
        {
            var result = await _mediator.Send(new FetchBlockchainDataCommand(chain, network), cancellationToken);
            _logger.LogInformation("Successfully fetched and stored fresh data for {Chain}/{Network}", chain, network);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fresh blockchain data from API for {Chain}/{Network}", chain, network);
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error fetching data from BlockCypher API: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches fresh data from BlockCypher API for all supported blockchains in parallel.
    /// Blockchains: eth/main, dash/main, btc/main, btc/test3, ltc/main.
    /// Stores each complete JSON response with CreatedAt timestamps in the database.
    /// Rate limited to 10 requests per minute.
    /// </summary>
    [HttpPost("fetch-all")]
    [EnableRateLimiting(RateLimitPolicies.Fetch)]
    [ProducesResponseType(typeof(IEnumerable<BlockchainDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BlockchainDataDto>>> FetchAllFreshBlockchainDataFromApi(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching fresh blockchain data from BlockCypher API for all chains in parallel");
        
        try
        {
            var result = await _mediator.Send(new FetchAllBlockchainsCommand(), cancellationToken);
            _logger.LogInformation("Successfully fetched and stored fresh data for all {Count} blockchains", result.Count());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fresh blockchain data from API for all chains");
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error fetching data from BlockCypher API: {ex.Message}");
        }
    }
}
