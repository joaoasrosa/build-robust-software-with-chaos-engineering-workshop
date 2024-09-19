using API.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Timeout;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FlightsController : Controller
{
    private readonly Routes _routes;
    private readonly ILogger<FlightsController> _logger;
    private readonly TimeoutPolicy _timeoutPolicy;

    public FlightsController(
        Routes routes, 
        IConfiguration configuration,
        ILogger<FlightsController> logger)
    {
        _routes = routes;
        _logger = logger;

        var timeoutInMilliseconds = configuration.GetValue<int>("PollySettings:TimeoutMilliseconds");

        _timeoutPolicy = Policy.Timeout(
            TimeSpan.FromMilliseconds(timeoutInMilliseconds),
            TimeoutStrategy.Pessimistic);
    }
    
    // GET: api/flights//routes?from={from}&to={to}
    [HttpGet("routes")]
    public IActionResult GetRoutes(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest("Missing IATA codes.");
        
        try
        {
            var result = _timeoutPolicy.Execute(() => _routes.GetRoutes(from, to));
            return Ok(result);
        }
        catch (TimeoutRejectedException timeoutRejectedException)
        {
            _logger.LogWarning(timeoutRejectedException, "Timeout rejected");
            return StatusCode(500, "The request timed out.");
        }
    }
}