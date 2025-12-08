using AutoMapper;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Queries;
using ICMarkets.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ICMarkets.Application.Handlers.Queries;

/// <summary>
/// Handler for getting all blockchain data.
/// Uses ReadDbContext to read from read replica for better scalability.
/// </summary>
public class GetAllBlockchainDataQueryHandler : IRequestHandler<GetAllBlockchainDataQuery, IEnumerable<BlockchainDataDto>>
{
    private readonly ReadDbContext _readContext;
    private readonly IMapper _mapper;

    public GetAllBlockchainDataQueryHandler(ReadDbContext readContext, IMapper mapper)
    {
        _readContext = readContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<BlockchainDataDto>> Handle(GetAllBlockchainDataQuery request, CancellationToken cancellationToken)
    {
        // Query reads from read replica
        var data = await _readContext.BlockchainData
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<BlockchainDataDto>>(data);
    }
}
