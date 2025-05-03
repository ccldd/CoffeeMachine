namespace CoffeeMachine.Services;

/// <summary>
/// A service to get the weather.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the current weather.
    /// </summary>
    /// <returns>The current weather.</returns>
    Task<WeatherData> GetCurrentWeatherAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The weather data.
/// </summary>
/// <param name="TemperatureCelsius">The temperature in Celsius.</param>
public record WeatherData(double TemperatureCelsius);