using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Microsoft.AspNetCore.Mvc.Testing;

namespace PaymentGateway.Api.Tests.Fixtures;

public class BankSimulatorFixture : IAsyncLifetime
{
    private readonly IContainer _container;

    private string BankUrl => $"http://localhost:{_container.GetMappedPublicPort(8080)}";

    public BankSimulatorFixture()
    {
        var impostersPath = Path.Combine(AppContext.BaseDirectory, "imposters");

        _container = new ContainerBuilder("bbyars/mountebank:2.8.1")
            .WithResourceMapping(impostersPath, "/imposters/")
            .WithCommand("--configfile", "/imposters/bank_simulator.ejs", "--allowInjection")
            .WithPortBinding(8080, true)
            .WithPortBinding(2525, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(2525)))
            .Build();
    }

    public HttpClient CreateClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("BankSimulator:BaseUrl", BankUrl);
            });

        return factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}