using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace thejevman.Api;

public class MessageFunction
{
    [Function("GetMessage")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "message")] HttpRequest req)
    {
        var name = req.Query["name"];
        var message = string.IsNullOrEmpty(name)
            ? "Hello from thejevman!"
            : $"Hello, {name}!";

        return new OkObjectResult(new { message });
    }
}
