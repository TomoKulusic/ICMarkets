using ICMarkets.Application.Commands;
using ICMarkets.Application.DTOs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ICMarkets.Application.Handlers.Commands;

public class FetchAllBlockchainsCommandHandler : IRequestHandler<FetchAllBlockchainsCommand, IEnumerable<BlockchainDataDto>>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FetchAllBlockchainsCommandHandler> _logger;

    private static readonly List<(string Chain, string Network)> BlockchainEndpoints = new()
    {
        ("eth", "main"),
        ("dash", "main"),
        ("btc", "main"),
        ("btc", "test3"),
        ("ltc", "main")
    };

    public FetchAllBlockchainsCommandHandler(IServiceScopeFactory serviceScopeFactory, ILogger<FetchAllBlockchainsCommandHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(FetchAllBlockchainsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching data for all blockchains in parallel");

        var tasks = BlockchainEndpoints.Select(endpoint => FetchBlockchainDataAsync(endpoint.Chain, endpoint.Network, cancellationToken));

        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("Successfully fetched data for {Count} blockchains", results.Length);

        return results;
    }

    private async Task<BlockchainDataDto> FetchBlockchainDataAsync(string chain, string network, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new FetchBlockchainDataCommand(chain, network), cancellationToken);
    }
}
