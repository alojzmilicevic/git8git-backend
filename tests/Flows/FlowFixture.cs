using core.Licensing;
using core.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using tests.Fakes;
using tests.Support;
using Xunit;

namespace tests.Flows;

public class FlowFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public FakeUsersStore FakeUsersStore { get; } = new();
    public FakeDomainsStore FakeDomainsStore { get; } = new();

    public ApiTestBrowser Browser { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var with = new With();
        with.Real<IUsersStore>(FakeUsersStore);
        with.Real<IDomainsStore>(FakeDomainsStore);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    with.Configure(services);
                });
            });

        _client = _factory.CreateClient();
        Browser = new ApiTestBrowser(_client);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    public void Reset()
    {
        FakeUsersStore.Clear();
        FakeDomainsStore.Clear();
    }
}
