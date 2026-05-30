using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using HealthChecks.UI.Client;

namespace Products.API.Extensions
{
    public static class MiddlewareExtensions
    {
        // Método de extensión para configurar todo el pipeline de Middlewares y rutas especiales
        public static void UseAppMiddleware(this WebApplication app)
        {
            // 1. Mecanismo de Correlation ID (Movido desde Program.cs para dejarlo limpio)
            app.Use(async (context, next) =>
            {
                string headerKey = "X-Correlation-Id";
                string correlationId = "";

                // Intentamos obtener la cabecera de la petición HTTP si existe
                if (context.Request.Headers.TryGetValue(headerKey, out var headerValue))
                {
                    correlationId = headerValue.ToString();
                }

                // Si no viene en la cabecera (o está vacía), generamos un nuevo ID único
                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = System.Guid.NewGuid().ToString();
                    
                    // Lo guardamos en los Request Headers para que los Exception Handlers lo lean
                    context.Request.Headers[headerKey] = correlationId;
                }

                // Lo guardamos en Items del contexto para que esté disponible durante toda la petición
                context.Items["CorrelationId"] = correlationId;

                // Lo agregamos a los Headers de la respuesta HTTP para que el cliente lo reciba
                context.Response.Headers[headerKey] = correlationId;

                // Continuamos con el siguiente paso en la ejecución de la petición
                await next();
            });

            // 2. Activamos el manejador global de excepciones obligatorio para usar IExceptionHandler
            app.UseExceptionHandler();

            // 3. Activamos Swagger UI si estamos en desarrollo
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // 4. Redirección HTTPS básica
            app.UseHttpsRedirection();

            // 5. Exposición del endpoint de salud detallado en formato JSON
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // 6. Exposición de la interfaz gráfica interactiva (Dashboard UI)
            app.MapHealthChecksUI(setup =>
            {
                setup.UIPath = "/health-ui";
            });
        }
    }
}
