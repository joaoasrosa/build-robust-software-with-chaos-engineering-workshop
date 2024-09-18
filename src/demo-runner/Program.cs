using System.Collections.Immutable;
using System.Diagnostics;
using Random = System.Random;

namespace demo_runner;

internal abstract class Program
{
    static async Task Main(string[] args)
    {
        // Token to signal when to stop the action
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Start the action in a background task
        var actionTask = CallApiUntilKeyPress(cancellationToken);

        // Wait for any key press
        Console.WriteLine("Press any key to stop the calls to the API...");
        Console.ReadKey();

        // Signal the action to stop
        await cancellationTokenSource.CancelAsync();

        // Wait for the action task to complete
        await actionTask;

        Console.WriteLine("API calls stopped.");
    }

    private static async Task CallApiUntilKeyPress(CancellationToken cancellationToken)
    {
        var airportCodes = await LoadAirportCodes();
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var stopwatch = new Stopwatch(); // Create a new stopwatch
            stopwatch.Start(); // Start the stopwatch

            try
            {
                var from = RandomPickAirport(airportCodes);
                var to = RandomPickAirport(airportCodes);
                var response = await client.GetAsync(
                    "http://localhost:5073/api/flights/routes?from={from}&to={to}",
                    cancellationToken);

                stopwatch.Stop(); // Stop the stopwatch after the API call
                Console.WriteLine($"API call took {stopwatch.ElapsedMilliseconds} ms, with HTTP status {response.StatusCode}. From {from} tp {to}.");
            }
            catch (TaskCanceledException)
            {
                // Handle the case where the task is canceled due to cancellationToken
                Console.WriteLine("API call canceled.");
                break;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
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
            if (string.IsNullOrWhiteSpace(line)) continue; // Skip empty lines

            var fields = line.Split(',');

            if (fields.Length <= 4 || string.IsNullOrWhiteSpace(fields[4])) 
                continue;
            
            var iataCode = fields[4].Replace("\"", "").Trim(); // Clean quotes and trim whitespace
            if (iataCode.Length == 3) // Ensure it's a valid IATA code
            {
                iataCodes.Add(iataCode);
            }
        }

        return iataCodes.ToImmutableArray();
    }
}