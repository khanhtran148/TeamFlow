using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TeamFlow.Api.HealthChecks;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            return HealthCheckResult.Healthy($"RabbitMQ is reachable at {host}:{port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"RabbitMQ is unreachable: {ex.Message}",
                exception: ex);
        }
    }
}
