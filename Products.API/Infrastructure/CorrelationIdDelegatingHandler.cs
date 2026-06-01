using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Products.API.Infrastructure
{
    // Un DelegatingHandler es como un middleware pero para llamadas HTTP salientes.
    // Intercepta las llamadas que hace nuestro HttpClient hacia otras APIs.
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Inyectamos el IHttpContextAccessor para poder acceder a la peticion HTTP actual.
        public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string headerKey = "X-Correlation-Id";

            // Obtenemos el HttpContext de la peticion que esta corriendo actualmente
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                string correlationId = "";

                // 1. Intentamos leer el CorrelationId guardado en context.Items
                if (httpContext.Items.ContainsKey("CorrelationId"))
                {
                    object? itemValue = httpContext.Items["CorrelationId"];
                    if (itemValue != null)
                    {
                        correlationId = itemValue.ToString() ?? "";
                    }
                }
                
                // 2. Si no estaba ahi, lo buscamos en los headers de la peticion entrante
                if (string.IsNullOrEmpty(correlationId))
                {
                    if (httpContext.Request.Headers.TryGetValue(headerKey, out var headerValue))
                    {
                        correlationId = headerValue.ToString();
                    }
                }

                // Si encontramos el CorrelationId, se lo inyectamos a la llamada HTTP saliente
                if (!string.IsNullOrEmpty(correlationId))
                {
                    // Evitamos duplicar la cabecera si ya existe en la peticion saliente
                    if (!request.Headers.Contains(headerKey))
                    {
                        request.Headers.Add(headerKey, correlationId);
                    }
                }
            }

            // Dejamos que la peticion HTTP saliente continúe su camino
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
