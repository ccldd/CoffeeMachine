using CoffeeMachine.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CoffeeMachine.Controllers;

/// <summary>
/// Controller for brewing coffee.
/// </summary>
[ApiController]
[Route("")]
public class CoffeeController(
    ICoffeeService coffeeService,
    ILogger<CoffeeController> logger,
    IWeatherService weatherService,
    IMemoryCache memoryCache)
    : ControllerBase
{
    private const string TemperatureCacheKey = "CurrentTemperature";
    internal static readonly TimeSpan TemperatureCacheExpiration = TimeSpan.FromMinutes(5);

    internal const string YourPipingHotCoffeeIsReady = "Your piping hot coffee is ready";
    internal const string YourRefreshingIcedCoffeeIsReady = "Your refreshing iced coffee is ready";
    internal const double MaxHotCoffeeTemperature = 30.0;

    /// <summary>
    /// Brews coffee.
    /// </summary>
    /// <returns>The result of brewing the coffee.</returns>
    /// <response code="200">The coffee was brewed successfully.</response>
    /// <response code="503">The coffee machine is out of coffee.</response>
    /// <response code="418">The coffee machine is not brewing coffee today.</response>
    [HttpGet("brew-coffee")]
    [ProducesResponseType<GetBrewCoffeeDto>(statusCode: StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status418ImATeapot)]
    public async Task<Results<StatusCodeHttpResult, Ok<GetBrewCoffeeDto>>> GetBrewCoffee(CancellationToken cancellationToken)
    {
        var brewCoffeeResult = coffeeService.BrewCoffee();
        return brewCoffeeResult switch
        {
            BrewCoffeeResult.Success(var Prepared) =>
                TypedResults.Ok(
                    new GetBrewCoffeeDto(
                        await GetSuccessMessageAsync(cancellationToken), Prepared)),
            BrewCoffeeResult.OutOfCoffee =>
                TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
            BrewCoffeeResult.NotBrewingCoffeeToday =>
                TypedResults.StatusCode(StatusCodes.Status418ImATeapot),
            _ => UnhandledBrewCoffeeResult(),
        };
    }

    private StatusCodeHttpResult UnhandledBrewCoffeeResult()
    {
        logger.LogWarning("Unhandled BrewCoffeeResult");
        return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
    }

    private async Task<string> GetSuccessMessageAsync(CancellationToken cancellationToken)
    {
        var message = YourPipingHotCoffeeIsReady;
        try
        {
            var temperature = await GetCurrentTemperatureAsync(cancellationToken);
            if (temperature > MaxHotCoffeeTemperature)
            {
                message = YourRefreshingIcedCoffeeIsReady;
            }
        }
        catch (Exception e)
        {
            // If we can't get the temperature, we still want to brew coffee.
            logger.LogWarning(e, "Failed to get weather data");
        }

        return message;
    }

    /// <summary>
    /// Retrieves the temperature from the weather service and caches it for 5 minutes.
    /// </summary>
    /// <returns>The temperature in Celsius</returns>
    private async Task<double> GetCurrentTemperatureAsync(CancellationToken cancellationToken)
    {
        var temp = await memoryCache.GetOrCreateAsync(TemperatureCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TemperatureCacheExpiration;
            var weather = await weatherService.GetCurrentWeatherAsync(cancellationToken);
            return weather.TemperatureCelsius;
        });

        return temp;
    }
}

/// <summary>
/// Response of <see cref="CoffeeController.GetBrewCoffee"/>.
/// </summary>
/// <param name="Message">The message.</param>
/// <param name="Prepared">The ISO 8601 timestamp of when the coffee was prepared.</param>
public record GetBrewCoffeeDto(string Message, DateTimeOffset Prepared);
