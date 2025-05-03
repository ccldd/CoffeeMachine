namespace CoffeeMachine.Services;

public interface ICoffeeService
{
    /// <summary>
    /// Brew a cup of coffee.
    /// </summary>
    /// <remarks>This must be thread-safe.</remarks>
    BrewCoffeeResult BrewCoffee();
}

public abstract record BrewCoffeeResult
{
    public record class Success(DateTimeOffset Prepared) : BrewCoffeeResult;
    public record class OutOfCoffee() : BrewCoffeeResult;
    public record class NotBrewingCoffeeToday() : BrewCoffeeResult;
}
