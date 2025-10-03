using System;

namespace Fcg.Identity.Shared.Models.Responses;

/// <summary>
/// Health check response from the identity service.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Service status (healthy, unhealthy, degraded).
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp da verificação.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Service version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}