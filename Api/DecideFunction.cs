using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace thejevman.Api;

public class DecideFunction
{
    private readonly ILogger<DecideFunction> _logger;

    public DecideFunction(ILogger<DecideFunction> logger) => _logger = logger;

    [Function("Decide")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "decide")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<DecideRequest>();

        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return new BadRequestResult();
        }

        _logger.LogInformation("Decide requested: {Question} with {Count} alternatives",
            request.Question, request.Alternatives?.Count ?? 0);

        return new OkObjectResult(new
        {
            question = request.Question,
            alternatives = request.Alternatives ?? new List<string>()
        });
    }

    public class DecideRequest
    {
        public string? Question { get; set; }

        public List<string>? Alternatives { get; set; }
    }
}
