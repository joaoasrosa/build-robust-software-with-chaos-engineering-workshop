using API.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FlightsController : Controller
{
    private readonly Routes _routes;

    public FlightsController(Routes routes)
    {
        _routes = routes;
    }
    
    // GET: api/flights//routes?from={from}&to={to}
    [HttpGet("routes")]
    public IActionResult GetRoutes(string from, string to)
    {
        return Ok(_routes.GetRoutes(from, to));
    }
}