using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Domain.Interfaces;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting the latest blockchain data entry.
/// Uses repository pattern for data access.
/// </summary>
public class GetLatestBlockchainDataQueryHandler : IRequestHandler<GetLatestBlockchainDataQuery, BlockchainDataDto?>
{
    private readonly IBlockchainRepository _repository;
    private readonly IMapper _mapper;

    public GetLatestBlockchainDataQueryHandler(IBlockchainRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<BlockchainDataDto?> Handle(GetLatestBlockchainDataQuery request, CancellationToken cancellationToken)
    {
        var data = await _repository.GetLatestByChainAsync(request.Chain, request.Network, cancellationToken);
        return data == null ? null : _mapper.Map<BlockchainDataDto>(data);
    }
}
