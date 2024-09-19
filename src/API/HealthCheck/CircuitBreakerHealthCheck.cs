using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;

namespace API.HealthCheck;

public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly CircuitBreakerPolicy _circuitBreakerPolicy;

    public CircuitBreakerHealthCheck(CircuitBreakerPolicy circuitBreakerPolicy)
    {
        _circuitBreakerPolicy = circuitBreakerPolicy;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var state = _circuitBreakerPolicy.CircuitState;
        var description = $"Circuit breaker is currently {state}.";

        switch (state)
        {
            case CircuitState.Closed:
                return Task.FromResult(HealthCheckResult.Healthy(description));
            case CircuitState.HalfOpen:
                return Task.FromResult(HealthCheckResult.Degraded(description));
            case CircuitState.Open:
                return Task.FromResult(HealthCheckResult.Unhealthy(description));
            default:
                return Task.FromResult(HealthCheckResult.Unhealthy("Unknown circuit breaker state."));
        }
    }
}