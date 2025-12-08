using ICMarkets.Application.DTOs;
using MediatR;

namespace ICMarkets.Application.Commands;

public record FetchBlockchainDataCommand(string Chain, string Network) : IRequest<BlockchainDataDto>;
