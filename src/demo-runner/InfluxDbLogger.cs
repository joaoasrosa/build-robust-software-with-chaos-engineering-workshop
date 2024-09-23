using System.Collections.Concurrent;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;

namespace demo_runner;

public class InfluxDbLogger : ILogger, IMetricsLogger
{
    private readonly string _categoryName;
    private readonly InfluxDBClient _influxClient;
    private readonly string _bucket;
    private readonly string _org;
    private readonly LogLevel _logLevel;

    public InfluxDbLogger(
        string categoryName,
        InfluxDBClient influxClient,
        string bucket,
        string org,
        LogLevel logLevel)
    {
        _categoryName = categoryName;
        _influxClient = influxClient;
        _bucket = bucket;
        _org = org;
        _logLevel = logLevel;
    }

    IDisposable? ILogger.BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _logLevel;
    }

    public async void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var logMessage = formatter(state, exception);
        var logLevelTag = logLevel.ToString();

        var point = PointData
            .Measurement("logs")
            .Tag("level", logLevelTag)
            .Tag("category", _categoryName)
            .Field("message", logMessage)
            .Field("event_id", eventId.Id)
            .Field("exception", exception?.ToString())
            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

        var writeApi = _influxClient.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org);
    }

    public async Task LogMetricAsync(
        string metricName,
        double value,
        string fieldName = "value",
        string? tagKey = null,
        string? tagValue = null)
    {
        var point = PointData
            .Measurement(metricName)
            .Field(fieldName, value)
            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

        if (tagKey != null && tagValue != null)
        {
            point.Tag(tagKey, tagValue);
        }

        var writeApi = _influxClient.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org);
    }
}

public class InfluxDbLoggerProvider : ILoggerProvider
{
    private readonly InfluxDBClient _influxClient;
    private readonly string _bucket;
    private readonly string _org;
    private readonly LogLevel _logLevel;

    private readonly ConcurrentDictionary<string, InfluxDbLogger> _loggers =
        new ConcurrentDictionary<string, InfluxDbLogger>();

    public InfluxDbLoggerProvider(
        string url,
        string token,
        string bucket,
        string org,
        LogLevel logLevel = LogLevel.Information)
    {
        _influxClient = new InfluxDBClient(url, token);
        _bucket = bucket;
        _org = org;
        _logLevel = logLevel;
    }

    public IMetricsLogger CreateMetricsLogger(string categoryName)
    {
        return new InfluxDbLogger(categoryName, _influxClient, _bucket, _org, _logLevel);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName,
            name => new InfluxDbLogger(name, _influxClient, _bucket, _org, _logLevel));
    }

    public void Dispose()
    {
        _influxClient.Dispose();
        _loggers.Clear();
    }
}