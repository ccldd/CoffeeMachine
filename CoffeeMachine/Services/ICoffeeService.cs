namespace CoffeeMachine.Services;

/// <summary>
/// The service for brewing coffee.
/// </summary>
public interface ICoffeeService
{
    /// <summary>
    /// Brew a cup of coffee.
    /// </summary>
    /// <remarks>This must be thread-safe.</remarks>
    BrewCoffeeResult BrewCoffee();
}

/// <summary>
/// Result of <see cref="ICoffeeService.BrewCoffee"/>
/// </summary>
public abstract record BrewCoffeeResult
{
    /// <summary>
    /// Coffee brewed successfully.
    /// </summary>
    /// <param name="Prepared">The time the coffee was brewed.</param>
    public record class Success(DateTimeOffset Prepared) : BrewCoffeeResult;

    /// <summary>
    /// Out of coffee.
    /// </summary>
    public record class OutOfCoffee() : BrewCoffeeResult;

    /// <summary>
    /// Not brewing coffee today.
    /// </summary>
    public record class NotBrewingCoffeeToday() : BrewCoffeeResult;
}
