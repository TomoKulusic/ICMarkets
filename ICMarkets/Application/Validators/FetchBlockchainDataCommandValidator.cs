using FluentValidation;
using ICMarkets.Application.Commands;

namespace ICMarkets.Application.Validators;

/// <summary>
/// Validator for FetchBlockchainDataCommand.
/// Ensures chain and network parameters are valid before processing.
/// </summary>
public class FetchBlockchainDataCommandValidator : AbstractValidator<FetchBlockchainDataCommand>
{
    private static readonly string[] ValidChains = { "eth", "btc", "dash", "ltc", "doge" };
    private static readonly string[] ValidNetworks = { "main", "test3", "test" };

    public FetchBlockchainDataCommandValidator()
    {
        RuleFor(x => x.Chain)
            .NotEmpty()
            .WithMessage("Chain is required.")
            .MaximumLength(10)
            .WithMessage("Chain must not exceed 10 characters.")
            .Must(chain => !string.IsNullOrWhiteSpace(chain) && ValidChains.Contains(chain.ToLower()))
            .WithMessage($"Chain must be one of: {string.Join(", ", ValidChains)}.");

        RuleFor(x => x.Network)
            .NotEmpty()
            .WithMessage("Network is required.")
            .MaximumLength(20)
            .WithMessage("Network must not exceed 20 characters.")
            .Must(network => !string.IsNullOrWhiteSpace(network) && ValidNetworks.Contains(network.ToLower()))
            .WithMessage($"Network must be one of: {string.Join(", ", ValidNetworks)}.");
    }
}
