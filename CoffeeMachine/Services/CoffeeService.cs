namespace CoffeeMachine.Services;

/// <inheritdoc cref="ICoffeeService"/>
public class CoffeeService(TimeProvider timeProvider) : ICoffeeService
{
    private int _brewCount = 0;

    /// <summary>
    /// Brew coffee!
    /// </summary>
    public BrewCoffeeResult BrewCoffee()
    {
        if (IsAprilFirst())
        {
            return new BrewCoffeeResult.NotBrewingCoffeeToday();
        }

        var currentBrewCount = Interlocked.Increment(ref _brewCount);
        if (currentBrewCount % 5 == 0)
        {
            return new BrewCoffeeResult.OutOfCoffee();
        }

        var prepared = timeProvider.GetUtcNow();
        var response = new BrewCoffeeResult.Success(prepared);
        return response;
    }

    private bool IsAprilFirst()
    {
        var now = timeProvider.GetUtcNow();
        var isApril = now.Month == 4;
        var isFirstDay = now.Day == 1;

        return isApril && isFirstDay;
    }
}
