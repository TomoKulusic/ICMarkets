using FluentAssertions;
using FluentValidation.TestHelper;
using ICMarkets.Application.Commands;
using ICMarkets.Application.Validators;

namespace ICMarkets.UnitTests.Validators;

/// <summary>
/// Unit tests for FetchBlockchainDataCommandValidator.
/// Tests validation rules for command parameters.
/// </summary>
public class FetchBlockchainDataCommandValidatorTests
{
    private readonly FetchBlockchainDataCommandValidator _validator;

    public FetchBlockchainDataCommandValidatorTests()
    {
        _validator = new FetchBlockchainDataCommandValidator();
    }

    [Theory]
    [InlineData("eth", "main")]
    [InlineData("btc", "main")]
    [InlineData("dash", "main")]
    [InlineData("ltc", "main")]
    [InlineData("btc", "test3")]
    public void Validate_WithValidChainAndNetwork_ShouldNotHaveValidationErrors(string chain, string network)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(chain, network);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyChain_ShouldHaveValidationError(string chain)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(chain, "main");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Chain)
            .WithErrorMessage("Chain is required.");
    }

    [Fact]
    public void Validate_WithNullChain_ShouldHaveValidationError()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(null!, "main");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Chain);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyNetwork_ShouldHaveValidationError(string network)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", network);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Network)
            .WithErrorMessage("Network is required.");
    }

    [Fact]
    public void Validate_WithNullNetwork_ShouldHaveValidationError()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Network);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("xrp")]
    public void Validate_WithInvalidChain_ShouldHaveValidationError(string chain)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(chain, "main");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Chain)
            .WithErrorMessage("Chain must be one of: eth, btc, dash, ltc, doge.");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("testnet")]
    public void Validate_WithInvalidNetwork_ShouldHaveValidationError(string network)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", network);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Network)
            .WithErrorMessage("Network must be one of: main, test3, test.");
    }

    [Fact]
    public void Validate_WithChainTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("verylongchainname", "main");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Chain);
    }

    [Fact]
    public void Validate_WithNetworkTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", "verylongnetworknamethatexceedslimit");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Network);
    }

    [Theory]
    [InlineData("ETH", "MAIN")]
    [InlineData("Eth", "Main")]
    [InlineData("eTh", "mAiN")]
    public void Validate_WithDifferentCasing_ShouldBeValid(string chain, string network)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(chain, network);

        // Act
        var result = _validator.TestValidate(command);

        // Assert - Should be valid because validator converts to lowercase
        result.ShouldNotHaveAnyValidationErrors();
    }
}
