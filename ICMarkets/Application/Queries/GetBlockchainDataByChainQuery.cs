using ICMarkets.Application.DTOs;
using MediatR;

namespace ICMarkets.Application.Queries;

public record GetBlockchainDataByChainQuery(string Chain, string? Network = null) : IRequest<IEnumerable<BlockchainDataDto>>;
