using CoffeeMachine.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Controllers;

/// <summary>
/// Controller for brewing coffee.
/// </summary>
[ApiController]
[Route("")]
public class CoffeeController(ICoffeeService coffeeService) : ControllerBase
{
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
    public Results<StatusCodeHttpResult, Ok<GetBrewCoffeeDto>> GetBrewCoffee()
    {
        var brewCoffeeResult = coffeeService.BrewCoffee();
        return brewCoffeeResult switch
        {
            BrewCoffeeResult.Success success =>
                TypedResults.Ok(new GetBrewCoffeeDto("Your piping hot coffee is ready", success.Prepared)),
            BrewCoffeeResult.OutOfCoffee =>
                TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
            BrewCoffeeResult.NotBrewingCoffeeToday =>
                TypedResults.StatusCode(StatusCodes.Status418ImATeapot),
            _ => TypedResults.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

/// <summary>
/// Response of <see cref="CoffeeController.GetBrewCoffee"/>.
/// </summary>
/// <param name="Message">The message.</param>
/// <param name="Prepared">The ISO 8601 timestamp of when the coffee was prepared.</param>
public record GetBrewCoffeeDto(string Message, DateTimeOffset Prepared);
