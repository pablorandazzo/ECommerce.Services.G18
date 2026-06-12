using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Json;

namespace Orders.API.Extensions
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

                // LOG DE CONSOLA: solo para Information o superior
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Information)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))

                // LOG DE ARCHIVO: Auditoria limpia de consumo de endpoints
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(le =>
                    {
                        var isSerilogMiddleware = Matching.FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(le);
                        if (!isSerilogMiddleware) return false;

                        if (le.Properties.TryGetValue("RequestPath", out var pathValue) &&
                            pathValue is ScalarValue scalar && scalar.Value is string path)
                        {
                            return !path.Contains("/health", System.StringComparison.OrdinalIgnoreCase) &&
                                   !path.Contains("/swagger", System.StringComparison.OrdinalIgnoreCase);
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
