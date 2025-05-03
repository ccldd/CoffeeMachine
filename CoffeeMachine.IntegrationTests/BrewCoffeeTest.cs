using CoffeeMachine.Controllers;
using CoffeeMachine.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace CoffeeMachine.IntegrationTests;

[TestClass]
public class BrewCoffeeTest
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ISystemClock> _systemClock;
    private readonly Mock<IWeatherService> _weatherService;
    private readonly WebApplicationFactory<Program> _factory;

    public BrewCoffeeTest()
    {
        _timeProvider = new FakeTimeProvider();
        _systemClock = new Mock<ISystemClock>();

        // Mock weather service, we don't want to be doing external calls
        _weatherService = new Mock<IWeatherService>();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<TimeProvider>(_timeProvider);
                    services.AddTransient(sp => _weatherService.Object);
                    services.AddMemoryCache(opts =>
                    {
                        opts.Clock = _systemClock.Object;
                    });
                });
            });
    }

    [TestMethod]
    [DataRow(CoffeeController.MaxHotCoffeeTemperature, CoffeeController.YourPipingHotCoffeeIsReady)]
    [DataRow(CoffeeController.MaxHotCoffeeTemperature + 0.0001, CoffeeController.YourRefreshingIcedCoffeeIsReady)]
    public async Task CoffeeBrew_Success_Returns200(double temperature, string expectedMessage)
    {
        _weatherService
            .Setup(x => x.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherData(temperature))
            .Verifiable();

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/brew-coffee");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.IsNotNull(json);
        Assert.AreEqual(expectedMessage, json["message"]?.ToString());

        var actualPrepared = DateTimeOffset.Parse(json["prepared"]?.ToString() ?? "");
        Assert.AreEqual(_timeProvider.GetUtcNow(), actualPrepared);

        _weatherService.VerifyAll();
    }

    /// <summary>
    /// This tests that we still return 200 OK even if the weather service is down.
    /// </summary>
    [TestMethod]
    public async Task CoffeeBrew_WeatherServiceDown_Returns200()
    {
        _weatherService
            .Setup(x => x.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()))
            .Throws(new WeatherServiceException(""))
            .Verifiable();

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/brew-coffee");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.IsNotNull(json);
        Assert.AreEqual(CoffeeController.YourPipingHotCoffeeIsReady, json["message"]?.ToString());

        var actualPrepared = DateTimeOffset.Parse(json["prepared"]?.ToString() ?? "");
        Assert.AreEqual(_timeProvider.GetUtcNow(), actualPrepared);

        _weatherService.VerifyAll();
    }

    [TestMethod]
    public async Task CoffeeBrew_Returns503_EveryFifthCall()
    {
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

    [TestMethod]
    public async Task CoffeeBrew_CachesWeather()
    {
        var now = DateTimeOffset.UtcNow;
        _systemClock.Setup(x => x.UtcNow)
            .Returns(now);

        _weatherService
            .Setup(x => x.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherData(20.0))
            .Verifiable(Times.Once());

        var client = _factory.CreateClient();

        await client.GetAsync("/brew-coffee");
        await client.GetAsync("/brew-coffee");

        _weatherService.VerifyAll();

        // the cache should be invalidated after some time
        _weatherService.Invocations.Clear();
        _systemClock.Setup(x => x.UtcNow)
            .Returns(now + CoffeeController.TemperatureCacheExpiration);
        await client.GetAsync("/brew-coffee");

        _weatherService.VerifyAll();
    }
}