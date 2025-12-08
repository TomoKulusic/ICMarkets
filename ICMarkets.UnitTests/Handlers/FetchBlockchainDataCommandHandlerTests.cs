using AutoMapper;
using FluentAssertions;
using ICMarkets.Application.Commands;
using ICMarkets.Application.DTOs;
using ICMarkets.Application.Handlers.Commands;
using ICMarkets.Domain.Entities;
using ICMarkets.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ICMarkets.UnitTests.Handlers;

/// <summary>
/// Unit tests for FetchBlockchainDataCommandHandler.
/// Tests the handler in isolation using mocks.
/// </summary>
public class FetchBlockchainDataCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBlockchainRepository> _mockRepository;
    private readonly Mock<IBlockCypherClient> _mockBlockCypherClient;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<FetchBlockchainDataCommandHandler>> _mockLogger;
    private readonly FetchBlockchainDataCommandHandler _handler;

    public FetchBlockchainDataCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockRepository = new Mock<IBlockchainRepository>();
        _mockBlockCypherClient = new Mock<IBlockCypherClient>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<FetchBlockchainDataCommandHandler>>();

        // Setup UnitOfWork to return the mock repository
        _mockUnitOfWork.Setup(x => x.BlockchainRepository).Returns(_mockRepository.Object);

        _handler = new FetchBlockchainDataCommandHandler(
            _mockUnitOfWork.Object,
            _mockBlockCypherClient.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldFetchAndStoreData()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", "main");
        var jsonResponse = "{\"name\":\"ETH.main\",\"height\":123456}";
        var savedEntity = new BlockchainData
        {
            Id = 1,
            Chain = "eth",
            Network = "main",
            RawJsonData = jsonResponse,
            CreatedAt = DateTime.UtcNow
        };
        var expectedDto = new BlockchainDataDto
        {
            Id = 1,
            Chain = "eth",
            Network = "main",
            RawJsonData = jsonResponse,
            CreatedAt = savedEntity.CreatedAt
        };

        _mockBlockCypherClient
            .Setup(x => x.GetBlockchainDataAsync("eth", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<BlockchainData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper
            .Setup(x => x.Map<BlockchainDataDto>(It.IsAny<BlockchainData>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Chain.Should().Be("eth");
        result.Network.Should().Be("main");
        result.RawJsonData.Should().Be(jsonResponse);

        _mockBlockCypherClient.Verify(
            x => x.GetBlockchainDataAsync("eth", "main", It.IsAny<CancellationToken>()),
            Times.Once
        );
        
        _mockRepository.Verify(
            x => x.AddAsync(It.Is<BlockchainData>(d => 
                d.Chain == "eth" && 
                d.Network == "main" && 
                d.RawJsonData == jsonResponse), 
                It.IsAny<CancellationToken>()),
            Times.Once
        );
        
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_BlockCypherClientThrowsException_ShouldPropagateException()
    {
        // Arrange
        var command = new FetchBlockchainDataCommand("eth", "main");
        _mockBlockCypherClient
            .Setup(x => x.GetBlockchainDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _handler.Handle(command, CancellationToken.None)
        );
        
        // Verify repository was never called since API failed
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<BlockchainData>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData("btc", "main")]
    [InlineData("eth", "test")]
    [InlineData("dash", "main")]
    public async Task Handle_DifferentChainNetworkCombinations_ShouldFetchCorrectData(string chain, string network)
    {
        // Arrange
        var command = new FetchBlockchainDataCommand(chain, network);
        var jsonResponse = $"{{\"name\":\"{chain}.{network}\"}}";
        var savedEntity = new BlockchainData
        {
            Id = 1,
            Chain = chain,
            Network = network,
            RawJsonData = jsonResponse,
            CreatedAt = DateTime.UtcNow
        };

        _mockBlockCypherClient
            .Setup(x => x.GetBlockchainDataAsync(chain, network, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<BlockchainData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockMapper
            .Setup(x => x.Map<BlockchainDataDto>(It.IsAny<BlockchainData>()))
            .Returns(new BlockchainDataDto { Chain = chain, Network = network, RawJsonData = jsonResponse });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Chain.Should().Be(chain);
        result.Network.Should().Be(network);
        
        _mockRepository.Verify(
            x => x.AddAsync(It.Is<BlockchainData>(d => d.Chain == chain && d.Network == network), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
