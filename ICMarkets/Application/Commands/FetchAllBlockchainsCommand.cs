using ICMarkets.Application.DTOs;
using MediatR;

namespace ICMarkets.Application.Commands;

public record FetchAllBlockchainsCommand : IRequest<IEnumerable<BlockchainDataDto>>;
