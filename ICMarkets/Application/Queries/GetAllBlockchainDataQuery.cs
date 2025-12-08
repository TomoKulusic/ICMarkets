using ICMarkets.Application.DTOs;
using MediatR;

namespace ICMarkets.Application.Queries;

public record GetAllBlockchainDataQuery : IRequest<IEnumerable<BlockchainDataDto>>;
