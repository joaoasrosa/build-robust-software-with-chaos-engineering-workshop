using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FlightsController : Controller
{
    private readonly IDbConnection _connection;

    public FlightsController(IDbConnection connection)
    {
        _connection = connection;
    }
    
    // GET: api/flights//routes?from={from}&to={to}
    [HttpGet("routes")]
    public IActionResult GetRoutes(string from, string to)
    {
        var routes = _connection.Query<Route>(
            "SELECT source_airport.iata AS 'From', destination_airport.iata AS 'To', airlines.name AS 'Airline' "+
            "FROM routes "+
            "INNER JOIN airports source_airport on routes.src_apid = source_airport.apid "+
            "INNER JOIN airports destination_airport on routes.dst_apid = destination_airport.apid "+
            "INNER JOIN airlines on routes.alid = airlines.alid "+
            "WHERE source_airport.iata = @From AND destination_airport.iata = @To",
        new {From = from, To = to}
            );
        
        return Ok(routes);
    }
    
    // Define a record for the route response (dummy data)
    public record Route(string From, string To, string Airline);
}