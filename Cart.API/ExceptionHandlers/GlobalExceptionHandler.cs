using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Este handler captura TODO lo que llegue hasta aquÃ­
            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Error Interno del Servidor";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            problemDetails.Detail = "Ocurrió un error inesperado. Por favor, contacte al administrador.";
            problemDetails.Instance = httpContext.Request.Path;

            // Usamos cÃ³digos genÃ©ricos para errores no previstos
            problemDetails.Extensions.Add("errorCode", "GEN-001");
            problemDetails.Extensions.Add("errorMessage", "Error interno no controlado.");
            
            string correlationId = "";
            if (httpContext.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            }
            problemDetails.Extensions.Add("correlationId", correlationId);

            Serilog.Log.Error(exception, "Error inesperado no controlado. CorrelationId: {CorrelationId}, Path: {Instance}", correlationId, problemDetails.Instance);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // Siempre devolvemos true porque es el Ãºltimo handler de la cadena (red de seguridad)
            return true;
        }
    }
}


