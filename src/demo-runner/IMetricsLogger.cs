namespace demo_runner;

public interface IMetricsLogger
{
    Task LogMetricAsync(
        string metricName,
        double value,
        string fieldName = "value",
        string? tagKey = null,
        string? tagValue = null);
}