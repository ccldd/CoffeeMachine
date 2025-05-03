using CoffeeMachine.Services;
using Microsoft.Extensions.Time.Testing;
using System.Collections.Concurrent;

namespace CoffeeMachine.UnitTests
{
    [TestClass]
    public class CoffeeServiceTest
    {
        private readonly FakeTimeProvider _timeProvider;
        private readonly CoffeeService _coffeeService;

        public CoffeeServiceTest()
        {
            _timeProvider = new FakeTimeProvider();
            _coffeeService = new CoffeeService(_timeProvider);
        }

        [TestMethod]
        public void TestBrewCoffee_ReturnsSuccess()
        {
            var fakeNow = DateTimeOffset.Parse("2000-01-01T00:00Z");
            _timeProvider.SetUtcNow(fakeNow);

            var result = _coffeeService.BrewCoffee();

            Assert.IsInstanceOfType(result, typeof(BrewCoffeeResult.Success));
            var successResult = result as BrewCoffeeResult.Success;
            Assert.IsNotNull(successResult);

            Assert.AreEqual(fakeNow, successResult.Prepared);
        }

        [TestMethod]
        public void TestBrewCoffee_OnEveryFifthCall_ReturnsOutOfCoffee()
        {
            // Testing 5th coffee
            for (int i = 0; i < 4; i++)
            {
                _coffeeService.BrewCoffee();
            }

            var result = _coffeeService.BrewCoffee();

            Assert.IsInstanceOfType(result, typeof(BrewCoffeeResult.OutOfCoffee));
            var outOfCoffeeResult = result as BrewCoffeeResult.OutOfCoffee;
            Assert.IsNotNull(outOfCoffeeResult);

            // Testing 10th coffee
            for (int i = 0; i < 4; i++)
            {
                _coffeeService.BrewCoffee();
            }

            result = _coffeeService.BrewCoffee();

            Assert.IsInstanceOfType(result, typeof(BrewCoffeeResult.OutOfCoffee));
            outOfCoffeeResult = result as BrewCoffeeResult.OutOfCoffee;
            Assert.IsNotNull(outOfCoffeeResult);
        }

        [TestMethod]
        public void TestBrewCoffee_IsThreadSafe()
        {
            // While we cannot guarantee thread race with unit tests, we can simulate it
            var numThreads = 10;
            var numBrewsPerThread = 1000;
            var totalBrews = numThreads * numBrewsPerThread;

            var results = new ConcurrentBag<BrewCoffeeResult>();
            var threads = new Thread[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < numBrewsPerThread; j++)
                    {
                        var result = _coffeeService.BrewCoffee();
                        results.Add(result);
                    }
                });
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.AreEqual(totalBrews, results.Count);

            var outOfCoffeeCount = results.Count(r => r is BrewCoffeeResult.OutOfCoffee);
            Assert.AreEqual(totalBrews / 5, outOfCoffeeCount);
        }

        [TestMethod]
        [DataRow("2000-04-01T00:00Z")]
        [DataRow("2000-04-01T12:00Z")]
        [DataRow("2000-04-01T12:59:59.9999Z")]
        public void TestBrewCoffee_OnAprilFirst_ReturnsNotBrewingCoffeeToday(string dateTime)
        {
            var fakeNow = DateTimeOffset.Parse(dateTime);
            _timeProvider.SetUtcNow(fakeNow);

            var result = _coffeeService.BrewCoffee();

            Assert.IsInstanceOfType(result, typeof(BrewCoffeeResult.NotBrewingCoffeeToday));
            var notBrewingResult = result as BrewCoffeeResult.NotBrewingCoffeeToday;
            Assert.IsNotNull(notBrewingResult);
        }
    }
}