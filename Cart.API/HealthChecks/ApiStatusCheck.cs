using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.HealthChecks
{
    public class ApiStatusCheck : IHealthCheck
    {
        // Guardamos el momento en que se inicia la API
        private static readonly DateTime StartTime = DateTime.UtcNow;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Calculamos el tiempo activo de la API
            TimeSpan uptime = DateTime.UtcNow - StartTime;
            string runtimeVersion = Environment.Version.ToString();

            // Guardamos la informaciÃ³n en un diccionario para la respuesta detallada
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("runtime", ".NET " + runtimeVersion);
            data.Add("uptime", uptime.ToString(@"hh\:mm\:ss"));
            data.Add("startedAt", StartTime.ToString("o"));

            // Devolvemos el estado saludable con la informaciÃ³n
            return Task.FromResult(
                HealthCheckResult.Healthy("API operativa â€” .NET " + runtimeVersion, data)
            );
        }
    }
}

