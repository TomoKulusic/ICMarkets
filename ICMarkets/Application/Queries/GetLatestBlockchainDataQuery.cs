using ICMarkets.Application.DTOs;
using MediatR;

namespace ICMarkets.Application.Queries;

public record GetLatestBlockchainDataQuery(string Chain, string? Network = null) : IRequest<BlockchainDataDto?>;
