using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Json;

namespace Products.API.Extensions
{
    public static class LoggingExtensions
    {
        public static void AddAppLogging(this WebApplicationBuilder builder)
        {
            var serviceName = builder.Configuration["ServiceName"] ?? builder.Environment.ApplicationName;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)

                // ALUMNO: LOG DE CONSOLA: libre para usarse como quieran
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Information) // Cambiado a Information para que los alumnos vean logs de inicio
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))

                // ALUMNO: LOG DE CONSOLA: Solo registra cuando se consume un endpoint, es una auditoría limpia. 
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(le =>
                    {
                        // Solo aceptar logs que vengan del Middleware de Serilog
                        // Esto elimina automáticamente los duplicados de Microsoft.AspNetCore.Mvc
                        var isSerilogMiddleware = Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(le);
                        if (!isSerilogMiddleware) return false;

                        // Excluir rutas irrelevantes
                        if (le.Properties.TryGetValue("RequestPath", out var pathValue) &&
                            pathValue is ScalarValue scalar && scalar.Value is string path)
                        {
                            return !path.Contains("/health", StringComparison.OrdinalIgnoreCase) &&
                                   !path.Contains("/swagger", StringComparison.OrdinalIgnoreCase);
                        }

                        return true;
                    })
                    .WriteTo.File(
                        path: "logs/audit.log",
                        formatter: new JsonFormatter(renderMessage: true),
                        rollingInterval: RollingInterval.Day))
                .CreateLogger();

            builder.Host.UseSerilog();
        }
    }
}
