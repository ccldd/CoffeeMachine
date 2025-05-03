using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CoffeeMachine.IntegrationTests;

[TestClass]
public class BrewCoffeeTest
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly WebApplicationFactory<Program> _factory;

    public BrewCoffeeTest()
    {
        _timeProvider = new FakeTimeProvider();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<TimeProvider>(_timeProvider);
                });
            });
    }

    [TestMethod]
    public async Task CoffeeBrew_Returns200()
    {
        var fakeNow = DateTimeOffset.Parse("2000-01-02T00:00Z");
        _timeProvider.SetUtcNow(fakeNow);
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/brew-coffee");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.IsNotNull(json);
        Assert.AreEqual("Your piping hot coffee is ready", json["message"]?.ToString());

        var actualPrepared = DateTimeOffset.Parse(json["prepared"]?.ToString() ?? "");
        Assert.AreEqual(fakeNow, actualPrepared);
    }

    [TestMethod]
    public async Task CoffeeBrew_Returns503_EveryFifthCall()
    {
        var fakeNow = DateTimeOffset.Parse("2000-01-02T00:00Z");
        _timeProvider.SetUtcNow(fakeNow);
        var client = _factory.CreateClient();

        HttpResponseMessage response;
        for (int i = 0; i < 4; i++)
        {
            response = await client.GetAsync("/brew-coffee");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        response = await client.GetAsync("/brew-coffee");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual(0, (await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [TestMethod]
    public async Task CoffeeBrew_Returns413_OnFirstOfApril()
    {
        var fakeNow = DateTimeOffset.Parse("2000-04-01T00:00Z");
        _timeProvider.SetUtcNow(fakeNow);
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/brew-coffee");
        Assert.AreEqual(418, (int)response.StatusCode);
        Assert.AreEqual(0, (await response.Content.ReadAsByteArrayAsync()).Length);
    }
}