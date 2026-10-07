using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace thejevman.Api;

public class WeatherFunction
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly ILogger<WeatherFunction> _logger;

    public WeatherFunction(ILogger<WeatherFunction> logger) => _logger = logger;

    [Function("GetWeather")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weather")] HttpRequest req)
    {
        _logger.LogInformation("Weather forecast requested.");

        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();

        return new OkObjectResult(forecasts);
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }
    }
}
