using System.Diagnostics;

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
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var stopwatch = new Stopwatch(); // Create a new stopwatch
            stopwatch.Start(); // Start the stopwatch

            try
            {
                var json = await client.GetStringAsync(
                    "http://localhost:5073/api/flights/routes?from=OPO&to=LIS", 
                    cancellationToken
                );

                stopwatch.Stop(); // Stop the stopwatch after the API call
                Console.WriteLine($"API call took {stopwatch.ElapsedMilliseconds} ms");
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
}