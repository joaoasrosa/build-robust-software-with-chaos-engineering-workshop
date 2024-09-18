using System.Collections.Immutable;
using System.Data;
using Dapper;
using Polly;
using Polly.Retry;

namespace API.DataAccess;

public class Routes
{
    private readonly IDbConnection _connection;
    private readonly RetryPolicy _retryPolicy;

    public Routes(
        IDbConnection connection,
        IConfiguration configuration,
        ILogger<Routes> logger)
    {
        _connection = connection;

        var retryCount = configuration.GetValue<int>("PollySettings:Retry:Count");
        var baseDelayMilliseconds = configuration.GetValue<int>("PollySettings:Retry:BaseDelayMilliseconds");

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: retryCount,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(baseDelayMilliseconds * Math.Pow(2, attempt - 1)),
                onRetry: (exception, timeSpan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        $"Retry {retryAttempt} after {timeSpan.TotalMilliseconds} ms due to: {exception.Message}");
                }
            );
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
            new { From = from, To = to }
        ).ToList());
    }
}