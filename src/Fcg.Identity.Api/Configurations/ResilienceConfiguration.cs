using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Fcg.Identity.Api.Configurations;

public static class ResilienceConfiguration
{
    private const int CircuitBreakerFailureThreshold = 5;
    private const int CircuitBreakerDurationSeconds = 30;
    private const int RetryMaxAttempts = 3;
    private const int RetryInitialDelaySeconds = 2;
    private const int TimeoutSeconds = 30;
    private const int BulkheadMaxParallelization = 10;
    private const int BulkheadMaxQueueing = 20;

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
    {
        return Policy.WrapAsync(
            GetBulkheadPolicy(logger),
            GetCircuitBreakerPolicy(logger),
            GetRetryPolicy(logger),
            GetTimeoutPolicy(logger)
        );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(CircuitBreakerDurationSeconds),
                onBreak: (outcome, breakDelay) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString();
                    logger.LogWarning(
                        "Circuit breaker opened for {DurationSeconds}s. Reason: {Reason}",
                        breakDelay.TotalSeconds,
                        reason
                    );
                },
                onReset: () => logger.LogInformation("Circuit breaker reset and closed"),
                onHalfOpen: () =>
                    logger.LogInformation("Circuit breaker half-open, testing connection")
            );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: RetryMaxAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(RetryInitialDelaySeconds, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString();
                    logger.LogInformation(
                        "Retry attempt {RetryCount} after {DelaySeconds}s. Reason: {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        reason
                    );
                }
            );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ILogger logger)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(TimeoutSeconds),
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger.LogWarning(
                    "Request timed out after {TimeoutSeconds}s",
                    timespan.TotalSeconds
                );
                return Task.CompletedTask;
            }
        );
    }

    private static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(ILogger logger)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: BulkheadMaxParallelization,
            maxQueuingActions: BulkheadMaxQueueing,
            onBulkheadRejectedAsync: context =>
            {
                logger.LogWarning(
                    "Request rejected by bulkhead policy. Max parallel: {MaxParallel}, Max queue: {MaxQueue}",
                    BulkheadMaxParallelization,
                    BulkheadMaxQueueing
                );
                return Task.CompletedTask;
            }
        );
    }
}
