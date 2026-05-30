using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cart.API.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        private static readonly System.Collections.Generic.HashSet<string> AuditMethods =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE" };

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!AuditMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();
            var requestBody = await ReadBodyAsync(context.Request.Body);
            context.Request.Body.Position = 0;

            var originalResponseBody = context.Response.Body;
            using (var memStream = new MemoryStream())
            {
                context.Response.Body = memStream;

                await _next(context);

                memStream.Position = 0;
                var responseBody = await new StreamReader(memStream).ReadToEndAsync();

                memStream.Position = 0;
                await memStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                _logger.LogInformation(
                    "AUDIT {@Method} {@Path} {@StatusCode} {@RequestBody} {@ResponseBody}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    TryParseJson(requestBody),
                    TryParseJson(responseBody));
            }
        }

        private static async Task<string> ReadBodyAsync(Stream body)
        {
            using (var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static object? TryParseJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try { return JsonSerializer.Deserialize<object>(raw); }
            catch { return raw; }
        }
    }
}
