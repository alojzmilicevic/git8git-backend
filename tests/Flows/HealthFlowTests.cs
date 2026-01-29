using System.Net;
using Xunit;

namespace tests.Flows;

public class HealthFlowTests : IClassFixture<FlowFixture>
{
    private readonly FlowFixture _fixture;

    public HealthFlowTests(FlowFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _fixture.Browser.GetAsync<HealthResponse>("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal("ok", response.Data.Status);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
}
