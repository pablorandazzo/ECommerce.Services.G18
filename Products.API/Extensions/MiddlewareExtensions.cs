using Products.API.Middleware;
using Serilog;

namespace Products.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void UseAppMiddleware(this WebApplication app)
        {
            // Mecanismo de Correlation ID
            app.Use(async (context, next) =>
            {
                string headerKey = "X-Correlation-Id";
                string correlationId = "";

                // 1. Intentamos obtener la cabecera de la petición HTTP si existe
                if (context.Request.Headers.TryGetValue(headerKey, out var headerValue))
                {
                    correlationId = headerValue.ToString();
                }

                // 2. Si no viene en la cabecera (o está vacía), generamos un nuevo ID
                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = System.Guid.NewGuid().ToString();
                    
                    // Lo guardamos en los Request Headers para que los Exception Handlers lo lean
                    context.Request.Headers[headerKey] = correlationId;
                }

                // 3. Lo guardamos en Items del contexto para que esté disponible durante toda la petición
                context.Items["CorrelationId"] = correlationId;

                // 4. Lo agregamos a los Headers de la respuesta HTTP para que el cliente lo reciba
                context.Response.Headers[headerKey] = correlationId;

                // 5. Metemos el Correlation ID en el contexto de logs de Serilog para que aparezca en consola y archivos
                using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
                {
                    // 6. Continuamos con el siguiente paso en la ejecución de la petición
                    await next();
                }
            });

            app.UseExceptionHandler(); // Middleware obligatorio para usar IExceptionHandler

            // Request logging de Serilog estructurado (estilo MiniApi)
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, _, ex) =>
                    (ex != null) ? Serilog.Events.LogEventLevel.Error :
                        (httpContext.Request.Path.StartsWithSegments("/health"))
                            ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information;
            });

            // Middleware de Auditoría para registrar POST/PUT/DELETE
            app.UseMiddleware<AuditMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
        }
    }
}
