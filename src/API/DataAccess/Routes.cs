using System.Collections.Immutable;
using System.Data;
using Dapper;

namespace API.DataAccess;

public class Routes
{
    private readonly IDbConnection _connection;

    public Routes(IDbConnection connection)
    {
        _connection = connection;
    }

    public ImmutableArray<Route> GetRoutes(string from, string to)
    {
        return _connection.Query<Route>(
            "SELECT source_airport.iata AS 'From', destination_airport.iata AS 'To', airlines.name AS 'Airline' "+
            "FROM routes "+
            "INNER JOIN airports source_airport on routes.src_apid = source_airport.apid "+
            "INNER JOIN airports destination_airport on routes.dst_apid = destination_airport.apid "+
            "INNER JOIN airlines on routes.alid = airlines.alid "+
            "WHERE source_airport.iata = @From AND destination_airport.iata = @To",
            new {From = from, To = to}
        ).ToImmutableArray();
    }
}