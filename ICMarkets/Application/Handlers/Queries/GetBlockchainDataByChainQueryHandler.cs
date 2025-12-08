using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting blockchain data by chain with optional network filter.
/// Uses ReadDbContext to read from read replica for better scalability.
/// </summary>
public class GetBlockchainDataByChainQueryHandler : IRequestHandler<GetBlockchainDataByChainQuery, IEnumerable<BlockchainDataDto>>
{
    private readonly ReadDbContext _readContext;
    private readonly IMapper _mapper;

    public GetBlockchainDataByChainQueryHandler(ReadDbContext readContext, IMapper mapper)
    {
        _readContext = readContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(GetBlockchainDataByChainQuery request, CancellationToken cancellationToken)
    {
        // Build query with optional network filter
        var query = _readContext.BlockchainData
            .Where(b => b.Chain.ToLower() == request.Chain.ToLower());

        if (!string.IsNullOrWhiteSpace(request.Network))
        {
            query = query.Where(b => b.Network.ToLower() == request.Network.ToLower());
        }

        // Query reads from read replica
        var data = await query
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
            
        return _mapper.Map<IEnumerable<BlockchainDataDto>>(data);
    }
}
