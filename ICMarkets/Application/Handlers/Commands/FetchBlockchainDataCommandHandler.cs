using AutoMapper;
using ICMarkets.Application.Commands;
using ICMarkets.Application.DTOs;
using ICMarkets.Domain.Entities;
using ICMarkets.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ICMarkets.Application.Handlers.Commands;

/// <summary>
/// Handler for fetching blockchain data from BlockCypher API.
/// Uses IUnitOfWork for database operations.
/// </summary>
public class FetchBlockchainDataCommandHandler : IRequestHandler<FetchBlockchainDataCommand, BlockchainDataDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlockCypherClient _blockCypherClient;
    private readonly IMapper _mapper;
    private readonly ILogger<FetchBlockchainDataCommandHandler> _logger;

    public FetchBlockchainDataCommandHandler(
        IUnitOfWork unitOfWork,
        IBlockCypherClient blockCypherClient,
        IMapper mapper,
        ILogger<FetchBlockchainDataCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _blockCypherClient = blockCypherClient;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BlockchainDataDto> Handle(FetchBlockchainDataCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching blockchain data for {Chain}/{Network}", request.Chain, request.Network);

        // Get the raw JSON response from the API
        var jsonResponse = await _blockCypherClient.GetBlockchainDataAsync(request.Chain, request.Network, cancellationToken);

        // Store the raw JSON data as-is from the API
        var blockchainData = new BlockchainData
        {
            Chain = request.Chain,
            Network = request.Network,
            RawJsonData = jsonResponse,
            CreatedAt = DateTime.UtcNow
        };

        // Add using repository and save
        await _unitOfWork.BlockchainRepository.AddAsync(blockchainData, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully fetched and saved blockchain data for {Chain}/{Network}", request.Chain, request.Network);

        return _mapper.Map<BlockchainDataDto>(blockchainData);
    }
}
