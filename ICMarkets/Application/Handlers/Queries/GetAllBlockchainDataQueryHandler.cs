using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Domain.Interfaces;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting all blockchain data.
/// Uses repository pattern for data access.
/// </summary>
public class GetAllBlockchainDataQueryHandler : IRequestHandler<GetAllBlockchainDataQuery, IEnumerable<BlockchainDataDto>>
{
    private readonly IBlockchainRepository _repository;
    private readonly IMapper _mapper;

    public GetAllBlockchainDataQueryHandler(IBlockchainRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(GetAllBlockchainDataQuery request, CancellationToken cancellationToken)
    {
        var data = await _repository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BlockchainDataDto>>(data);
    }
}
