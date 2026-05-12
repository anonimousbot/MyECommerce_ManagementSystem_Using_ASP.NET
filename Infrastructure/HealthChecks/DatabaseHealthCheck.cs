using EMS.Persistence.Context;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EMS.Infrastructure.HealthChecks
{
    public sealed class DatabaseHealthCheck(EmsContext dbContext) : IHealthCheck
    {
        private readonly EmsContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Database connection failed.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed.", ex);
            }
        }
    }
}
