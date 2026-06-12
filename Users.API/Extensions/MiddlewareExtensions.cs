using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.UI.Client;
using Users.API.Middleware;
using Users.API.Data;
using Users.API.Extensions.Endpoints;
using Serilog;
using System;

namespace Users.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void UseAppMiddleware(this WebApplication app)
        {
            // 1. Mecanismo de Correlation ID
            app.Use(async (context, next) =>
            {
                string headerKey = "X-Correlation-Id";
                string correlationId = "";

                if (context.Request.Headers.TryGetValue(headerKey, out var headerValue))
                {
                    correlationId = headerValue.ToString();
                }

                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                    context.Request.Headers[headerKey] = correlationId;
                }

                context.Items["CorrelationId"] = correlationId;
                context.Response.Headers[headerKey] = correlationId;

                using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
                {
                    await next();
                }
            });

            // 2. Manejo Global de Excepciones
            app.UseExceptionHandler();

            // 3. Request Logging de Serilog estructurado (estilo MiniApi)
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, _, ex) =>
                    (ex != null) ? Serilog.Events.LogEventLevel.Error :
                        (httpContext.Request.Path.StartsWithSegments("/health"))
                            ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information;
            });

            // 4. Middleware de Auditoría para registrar POST/PUT/DELETE
            app.UseMiddleware<AuditMiddleware>();

            // 5. Swagger UI en desarrollo
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // 6. Mapeo de Health Checks y Health Checks UI
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecksUI(setup => 
            {
                setup.UIPath = "/health-ui";
            });

            // 7. Inicializar base de datos SQLite
            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetService<DatabaseInitializer>();
                dbInitializer?.Initialize();
            }

            // 8. Mapear Endpoints del catálogo de usuarios
            app.MapUserEndpoints();
        }
    }
}
