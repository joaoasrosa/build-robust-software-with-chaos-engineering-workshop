using System.Data;
using API.DataAccess;
using MySql.Data.MySqlClient;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var mySqlConnectionBuilder = new MySqlConnectionStringBuilder(connectionString)
    {
        UserID = builder.Configuration["ConnectionStrings:DefaultConnection:DB_USER"],
        Password = builder.Configuration["ConnectionStrings:DefaultConnection:DB_PASSWORD"]
    };
    return new MySqlConnection(mySqlConnectionBuilder.ConnectionString);
});

builder.Services.AddScoped<Routes>();

builder.Services.AddSingleton<Policy>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Routes>>();

    var retryCount = configuration.GetValue<int>("PollySettings:Retry:Count");
    var baseDelayMilliseconds = configuration.GetValue<int>("PollySettings:Retry:BaseDelayMilliseconds");
    var exceptionsAllowedBeforeBreaking = configuration.GetValue<int>("PollySettings:CircuitBreaker:ExceptionsAllowedBeforeBreaking");
    var durationOfBreakMilliseconds = configuration.GetValue<int>("PollySettings:CircuitBreaker:DurationOfBreakMilliseconds");

    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetry(
            retryCount: retryCount,
            sleepDurationProvider: attempt =>
                TimeSpan.FromMilliseconds(baseDelayMilliseconds * Math.Pow(2, attempt - 1)),
            onRetry: (exception, timeSpan, retryAttempt, context) =>
            {
                logger.LogWarning($"Retry {retryAttempt} after {timeSpan.TotalMilliseconds} ms due to: {exception.Message}");
            }
        );

    var circuitBreakerPolicy = Policy
        .Handle<Exception>()
        .CircuitBreaker(
            exceptionsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
            durationOfBreak: TimeSpan.FromMilliseconds(durationOfBreakMilliseconds),
            onBreak: (exception, timespan) =>
            {
                logger.LogWarning($"Circuit breaker opened for {timespan.TotalMilliseconds} ms due to: {exception.Message}");
            },
            onReset: () =>
            {
                logger.LogInformation("Circuit breaker reset.");
            },
            onHalfOpen: () =>
            {
                logger.LogInformation("Circuit breaker is half-open. Next call is a trial.");
            }
        );

    return Policy.Wrap(retryPolicy, circuitBreakerPolicy);
});

builder.Services.AddRouting(routingServices =>
{
    routingServices.LowercaseUrls = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();