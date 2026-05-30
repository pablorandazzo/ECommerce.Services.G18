using System.Text;
using System.Text.Json;

namespace Notifications.API.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        // Solo auditar operaciones de escritura
        private static readonly HashSet<string> AuditMethods =
            new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE" };

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Si no es una operaciÃ³n de escritura, pasar directo
            if (!AuditMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // â”€â”€ Capturar Request body â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            context.Request.EnableBuffering(); // permite releer el stream
            var requestBody = await ReadBodyAsync(context.Request.Body);
            context.Request.Body.Position = 0; // rebobinar para que el endpoint lo lea

            // â”€â”€ Capturar Response body â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var originalResponseBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context); // ejecutar el endpoint

            memStream.Position = 0;
            var responseBody = await new StreamReader(memStream).ReadToEndAsync();

            // Copiar la respuesta de vuelta al stream original
            memStream.Position = 0;
            await memStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            // â”€â”€ Escribir entrada de auditorÃ­a â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _logger.LogInformation(
                "AUDIT {@Method} {@Path} {@StatusCode} {@RequestBody} {@ResponseBody}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                TryParseJson(requestBody),
                TryParseJson(responseBody));
        }

        private static async Task<string> ReadBodyAsync(Stream body)
        {
            using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        // Deserializar para que Serilog lo guarde como objeto JSON (no como string)
        private static object? TryParseJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try { return JsonSerializer.Deserialize<object>(raw); }
            catch { return raw; }
        }
    }
}

