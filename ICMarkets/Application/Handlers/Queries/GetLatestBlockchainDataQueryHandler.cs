using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting the latest blockchain data entry.
/// Uses ReadDbContext to read from read replica for better scalability.
/// </summary>
public class GetLatestBlockchainDataQueryHandler : IRequestHandler<GetLatestBlockchainDataQuery, BlockchainDataDto?>
{
    private readonly ReadDbContext _readContext;
    private readonly IMapper _mapper;

    public GetLatestBlockchainDataQueryHandler(ReadDbContext readContext, IMapper mapper)
    {
        _readContext = readContext;
        _mapper = mapper;
    }

    public async Task<BlockchainDataDto?> Handle(GetLatestBlockchainDataQuery request, CancellationToken cancellationToken)
    {
        // Build query with optional network filter
        var query = _readContext.BlockchainData
            .Where(b => b.Chain.ToLower() == request.Chain.ToLower());

        if (!string.IsNullOrWhiteSpace(request.Network))
        {
            query = query.Where(b => b.Network.ToLower() == request.Network.ToLower());
        }

        // Query reads from read replica - get latest entry
        var data = await query
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
            
        return data == null ? null : _mapper.Map<BlockchainDataDto>(data);
    }
}
