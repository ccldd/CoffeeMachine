
using CoffeeMachine.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CoffeeMachine.Services
{
    /// <summary>
    /// Uses the free Open Meteo API to get the weather.
    /// </summary>
    public class WeatherService(HttpClient httpClient, IOptions<WeatherServiceOptions> options) : IWeatherService
    {
        /// <summary>
        /// Get the current weather.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="WeatherData"/></returns>
        /// <exception cref="WeatherServiceException">Failed to get the weather data.</exception>
        public async Task<WeatherData> GetCurrentWeatherAsync(CancellationToken cancellationToken)
        {
            try
            {
                var url = QueryHelpers.AddQueryString($"{options.Value.BaseUrl}/forecast", new Dictionary<string, string?>
                {
                    ["latitude"] = options.Value.Latitude.ToString(),
                    ["longitude"] = options.Value.Longitude.ToString(),
                    ["current"] = "temperature_2m",
                    ["timezone"] = "auto",
                });

                var response = await httpClient.GetFromJsonAsync<OpenMeteoForecastResponse>(url, cancellationToken);
                if (response is null)
                {
                    throw new WeatherServiceException("Failed to get weather data.");
                }

                var data = new WeatherData(response.Current.TemperatureCelsius);
                return data;
            }
            catch (Exception e)
            {
                throw new WeatherServiceException("Failed to get weather data.", e);
            }
        }
    }
}

/// <summary>
/// Options for <see cref="WeatherService"/>
/// </summary>
public class WeatherServiceOptions
{
    [Required]
    public required string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// The longitude of the location to get the weather for.
    /// </summary>
    [Required]
    public required double Longitude { get; init; } = 0;

    /// <summary>
    /// The latitude of the location to get the weather for.
    /// </summary>
    [Required]
    public required double Latitude { get; init; } = 0;
}

public class OpenMeteoForecastResponse
{
    public required OpenMeteoForecastResponseCurrent Current { get; set; }
}

public class OpenMeteoForecastResponseCurrent
{
    [JsonPropertyName("temperature_2m")]
    public required double TemperatureCelsius { get; set; }
}

/// <summary>
/// An exception thrown by <see cref="WeatherService"/> when an operation fails.
/// </summary>
public class WeatherServiceException : Exception
{
    public WeatherServiceException(string message) : base(message)
    {

    }

    public WeatherServiceException(string message, Exception innerException) : base(message, innerException)
    {

    }
}