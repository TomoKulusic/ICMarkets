using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Domain.Interfaces;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting blockchain data by chain with optional network filter.
/// Uses repository pattern for data access.
/// </summary>
public class GetBlockchainDataByChainQueryHandler : IRequestHandler<GetBlockchainDataByChainQuery, IEnumerable<BlockchainDataDto>>
{
    private readonly IBlockchainRepository _repository;
    private readonly IMapper _mapper;

    public GetBlockchainDataByChainQueryHandler(IBlockchainRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(GetBlockchainDataByChainQuery request, CancellationToken cancellationToken)
    {
        var data = await _repository.GetByChainAsync(request.Chain, request.Network, cancellationToken);
        return _mapper.Map<IEnumerable<BlockchainDataDto>>(data);
    }
}
