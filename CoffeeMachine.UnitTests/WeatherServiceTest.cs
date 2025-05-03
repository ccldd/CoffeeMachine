using CoffeeMachine.Services;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace CoffeeMachine.UnitTests;

[TestClass]
public class WeatherServiceTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly WeatherServiceOptions _options;
    private readonly WeatherService _service;

    public WeatherServiceTest()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandler.Object);
        _options = new WeatherServiceOptions
        {
            BaseUrl = "https://api.open-meteo.com/v1",
            Latitude = 0,
            Longitude = 0
        };
        _service = new WeatherService(_httpClient, Options.Create(_options));
    }

    [TestMethod]
    public async Task TestGetCurrentWeatherAsync_OnSuccess_ReturnsTemperature()
    {
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                    {
                      "current": {
                        "temperature_2m": 21.3
                      }
                    }
                    """)
            })
            .Verifiable();

        var data = await _service.GetCurrentWeatherAsync(default);
        Assert.AreEqual(21.3, data.TemperatureCelsius);

        _httpMessageHandler.VerifyAll();
    }

    [TestMethod]
    public async Task TestGetCurrentWeatherAsync_OnError_Throws()
    {
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
            }));

        await Assert.ThrowsExceptionAsync<WeatherServiceException>(async () =>
        {
            await _service.GetCurrentWeatherAsync(default);
        });
    }
}