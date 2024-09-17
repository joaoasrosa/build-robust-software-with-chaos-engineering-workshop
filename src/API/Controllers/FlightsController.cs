using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FlightsController : Controller
{
    // GET: api/flights//routes?from={from}&to={to}
    [HttpGet("routes")]
    public IActionResult GetRoutes(string from, string to)
    {
        // For now, return dummy data
        var dummyRoutes = new[]
        {
            new Route(from, to, "Flight 101"),
            new Route(from, to, "Flight 202")
        };

        return Ok(dummyRoutes);
    }
    
    // Define a record for the route response (dummy data)
    public record Route(string From, string To, string FlightNumber);
}