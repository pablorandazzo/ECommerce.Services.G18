using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.API.HealthChecks
{
    public class ApiStatusCheck : IHealthCheck
    {
        private static readonly DateTime StartTime = DateTime.UtcNow;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            TimeSpan uptime = DateTime.UtcNow - StartTime;
            string version = Environment.Version.ToString();

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("runtime", ".NET " + version);
            data.Add("uptime", uptime.ToString(@"hh\:mm\:ss"));
            data.Add("startedAt", StartTime.ToString("o"));

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "API operativa — .NET " + version,
                    data));
        }
    }
}
