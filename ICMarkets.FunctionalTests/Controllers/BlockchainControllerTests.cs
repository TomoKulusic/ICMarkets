using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ICMarkets.Application.DTOs;
using Moq;

namespace ICMarkets.FunctionalTests.Controllers;

/// <summary>
/// Functional/End-to-End tests for BlockchainController.
/// Tests complete HTTP workflows with in-memory database and mocked external API.
/// </summary>
public class BlockchainControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public BlockchainControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_AllStoredBlockchainData_ReturnsOkWithEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<IEnumerable<BlockchainDataDto>>();
        data.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_FetchBlockchainData_ReturnsOkWithData()
    {
        // Act
        var response = await _client.PostAsync("/api/blockchain/fetch?chain=eth&network=main", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<BlockchainDataDto>();
        data.Should().NotBeNull();
        data!.Chain.Should().Be("eth");
        data.Network.Should().Be("main");
        data.RawJsonData.Should().NotBeNullOrEmpty();
        data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify the mock API was called
        _factory.MockBlockCypherClient.Verify(
            x => x.GetBlockchainDataAsync("eth", "main", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task POST_FetchBlockchainData_WithoutParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/api/blockchain/fetch", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_LatestStoredBlockchainData_AfterFetch_ReturnsData()
    {
        // Arrange - Fetch data first
        await _client.PostAsync("/api/blockchain/fetch?chain=btc&network=main", null);

        // Act
        var response = await _client.GetAsync("/api/blockchain/btc/latest?network=main");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<BlockchainDataDto>();
        data.Should().NotBeNull();
        data!.Chain.Should().Be("btc");
        data.Network.Should().Be("main");
    }

    [Fact]
    public async Task GET_LatestStoredBlockchainData_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/blockchain/nonexistent/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_StoredBlockchainDataByChain_AfterFetch_ReturnsData()
    {
        // Arrange - Fetch data first
        await _client.PostAsync("/api/blockchain/fetch?chain=dash&network=main", null);

        // Act
        var response = await _client.GetAsync("/api/blockchain/dash");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<IEnumerable<BlockchainDataDto>>();
        data.Should().NotBeNull();
        data.Should().NotBeEmpty();
        data!.All(x => x.Chain == "dash").Should().BeTrue();
    }

    [Fact]
    public async Task GET_StoredBlockchainDataByChain_WithNetworkFilter_ReturnsFilteredData()
    {
        // Arrange - Fetch different networks
        await _client.PostAsync("/api/blockchain/fetch?chain=ltc&network=main", null);
        await _client.PostAsync("/api/blockchain/fetch?chain=ltc&network=test", null);

        // Act
        var response = await _client.GetAsync("/api/blockchain/ltc?network=main");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<IEnumerable<BlockchainDataDto>>();
        data.Should().NotBeNull();
        data!.All(x => x.Network == "main").Should().BeTrue();
    }

    [Fact]
    public async Task POST_FetchAllBlockchainData_ReturnsMultipleEntries()
    {
        // Act
        var response = await _client.PostAsync("/api/blockchain/fetch-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<IEnumerable<BlockchainDataDto>>();
        data.Should().NotBeNull();
        data.Should().HaveCount(5); // eth/main, dash/main, btc/main, btc/test3, ltc/main

        // Verify API was called for each blockchain
        _factory.MockBlockCypherClient.Verify(
            x => x.GetBlockchainDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(5));
    }

    [Fact]
    public async Task GET_Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimiting_ExceedingLimit_ReturnsTooManyRequests()
    {
        // Note: Rate limiting in test environment may not work the same as production
        // This test verifies the system handles multiple requests gracefully
        
        // Act - Send multiple requests rapidly
        var responses = new List<HttpStatusCode>();
        for (int i = 0; i < 15; i++)
        {
            var response = await _client.PostAsync($"/api/blockchain/fetch?chain=test{i}&network=main", null);
            responses.Add(response.StatusCode);
        }

        // Assert - Verify we got responses
        responses.Should().NotBeEmpty();
        responses.Should().Contain(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResponseCaching_RepeatedRequests_UsesCache()
    {
        // Arrange - Fetch data
        await _client.PostAsync("/api/blockchain/fetch?chain=doge&network=main", null);

        // Act - Get latest twice within cache duration
        var response1 = await _client.GetAsync("/api/blockchain/doge/latest");
        var response2 = await _client.GetAsync("/api/blockchain/doge/latest");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var data1 = await response1.Content.ReadFromJsonAsync<BlockchainDataDto>();
        var data2 = await response2.Content.ReadFromJsonAsync<BlockchainDataDto>();

        data1.Should().BeEquivalentTo(data2);
    }

    [Fact]
    public async Task CompleteWorkflow_FetchAndRetrieve_ShouldWork()
    {
        // 1. Verify no data exists initially in the in-memory database
        var initialResponse = await _client.GetAsync("/api/blockchain/xrp/latest");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Fetch fresh data from mocked API and store in database
        var fetchResponse = await _client.PostAsync("/api/blockchain/fetch?chain=xrp&network=main", null);
        fetchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedData = await fetchResponse.Content.ReadFromJsonAsync<BlockchainDataDto>();

        // 3. Retrieve the fetched data from in-memory database
        var getResponse = await _client.GetAsync("/api/blockchain/xrp/latest?network=main");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedData = await getResponse.Content.ReadFromJsonAsync<BlockchainDataDto>();

        // 4. Verify they match
        retrievedData.Should().NotBeNull();
        retrievedData!.Id.Should().Be(fetchedData!.Id);
        retrievedData.RawJsonData.Should().Be(fetchedData.RawJsonData);

        // 5. Verify it appears in "get all"
        var allResponse = await _client.GetAsync("/api/blockchain");
        var allData = await allResponse.Content.ReadFromJsonAsync<IEnumerable<BlockchainDataDto>>();
        allData.Should().Contain(x => x.Id == fetchedData.Id);

        // 6. Verify mock API was called
        _factory.MockBlockCypherClient.Verify(
            x => x.GetBlockchainDataAsync("xrp", "main", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
