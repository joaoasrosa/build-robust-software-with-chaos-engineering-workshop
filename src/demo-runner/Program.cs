using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Random = System.Random;

namespace demo_runner;

internal abstract class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<Program>()
            .Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, configuration);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var metricsLogger = serviceProvider.GetService<IMetricsLogger>();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var actionTask = CallApiUntilKeyPress(cancellationToken, metricsLogger ?? throw new InvalidOperationException());

        Console.WriteLine("Press any key to stop the calls to the API...");
        Console.ReadKey();

        await cancellationTokenSource.CancelAsync();

        await actionTask;

        Console.WriteLine("API calls stopped.");
    }

    private static async Task CallApiUntilKeyPress(
        CancellationToken cancellationToken,
        IMetricsLogger metricsLogger)
    {
        var airportCodes = await LoadAirportCodes();
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();

        while (!cancellationToken.IsCancellationRequested)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var from = RandomPickAirport(airportCodes);
                var to = RandomPickAirport(airportCodes);
                var response = await client.GetAsync(
                    "http://localhost:5073/api/flights/routes?from={from}&to={to}",
                    cancellationToken);

                stopwatch.Stop();

                if (metricsLogger != null)
                {
                    await metricsLogger.LogMetricAsync(
                        "api_call_duration",
                        stopwatch.ElapsedMilliseconds,
                        "duration");
                    
                    await metricsLogger.LogMetricAsync(
                        "api_call_http_status",
                        (int)response.StatusCode,
                        "http_status");
                }

                Console.WriteLine(
                    $"API call took {stopwatch.ElapsedMilliseconds} ms, with HTTP status {response.StatusCode}. From {from} to {to}.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("API call canceled.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    private static string RandomPickAirport(ImmutableArray<string> airportCodes)
    {
        var random = new Random();
        return airportCodes[random.Next(airportCodes.Length)];
    }

    private static async Task<ImmutableArray<string>> LoadAirportCodes()
    {
        Console.WriteLine("Loading the airport codes...");

        var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(
            "https://raw.githubusercontent.com/jpatokal/openflights/master/data/airports.dat");
        var lines = response.Split('\n');

        var iataCodes = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = line.Split(',');

            if (fields.Length <= 4 || string.IsNullOrWhiteSpace(fields[4]))
                continue;

            var iataCode = fields[4].Replace("\"", "").Trim();
            if (iataCode.Length == 3)
            {
                iataCodes.Add(iataCode);
            }
        }

        return iataCodes.ToImmutableArray();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var influxDbUrl = configuration["InfluxDB:Url"];
        var influxDbToken = configuration["InfluxDB:Token"];
        var influxDbOrg = configuration["InfluxDB:Org"];
        var influxDbBucket = configuration["InfluxDB:Bucket"];

        if (influxDbUrl == null || influxDbToken == null || influxDbBucket == null || influxDbOrg == null) return;
        
        var influxLoggerProvider = new InfluxDbLoggerProvider(
            url: influxDbUrl,
            token: influxDbToken,
            bucket: influxDbBucket,
            org: influxDbOrg,
            logLevel: LogLevel.Information);

        services.AddSingleton<ILoggerProvider>(influxLoggerProvider);
        services.AddSingleton<IMetricsLogger>(provider => influxLoggerProvider.CreateMetricsLogger("default"));
        services.AddLogging(builder => { builder.AddProvider(influxLoggerProvider); });
    }
}