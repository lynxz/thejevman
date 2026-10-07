using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace thejevman.Api;

public class DecideFunction
{
    private const string JevEndpoint = "https://api.typesafe.ai/v1/systemone";
    private const string JevModel = "jev-latest";

    private static readonly HttpClient HttpClient = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<DecideFunction> _logger;

    public DecideFunction(IConfiguration configuration, ILogger<DecideFunction> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("Decide")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "decide")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<DecideRequest>();

        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return new BadRequestResult();
        }

        var options = (request.Alternatives ?? new List<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct()
            .ToList();

        if (options.Count == 0)
        {
            return new BadRequestObjectResult(new { error = "At least one alternative is required." });
        }

        var apiKey = _configuration["Jev:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Jev API key is not configured. Set Jev:ApiKey in appsettings.json.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        var jevRequest = new
        {
            state = request.Question,
            model = JevModel,
            questions = new Dictionary<string, object>
            {
                ["decision"] = new
                {
                    type = "choice",
                    instructions = "Choose the most appropriate outcome for the user's question.",
                    criteria = options.ToDictionary(option => option, _ => (object?)null)
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, JevEndpoint)
        {
            Content = JsonContent.Create(jevRequest)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await HttpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Jev request failed with {StatusCode}: {Body}", response.StatusCode, body);
            return new StatusCodeResult((int)response.StatusCode);
        }

        var jevResponse = JsonSerializer.Deserialize<JevResponse>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (jevResponse?.Answers is null ||
            !jevResponse.Answers.TryGetValue("decision", out var answer))
        {
            _logger.LogError("Unexpected Jev response: {Body}", body);
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }

        return new OkObjectResult(answer);
    }

    public class DecideRequest
    {
        public string? Question { get; set; }

        public List<string>? Alternatives { get; set; }
    }

    public class JevResponse
    {
        public string? Model { get; set; }

        public Dictionary<string, JevAnswer>? Answers { get; set; }
    }

    public class JevAnswer
    {
        public string? Type { get; set; }

        public string? Choice { get; set; }

        public double Confidence { get; set; }

        public Dictionary<string, double>? Probabilities { get; set; }
    }
}
