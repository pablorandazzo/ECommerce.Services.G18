using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Orders.API.ExceptionHandlers
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
            problemDetails.Detail = "OcurriÃ³ un error inesperado. Por favor, contacte al administrador.";
            problemDetails.Instance = httpContext.Request.Path;

            // Usamos el cÃ³digo de error interno del catÃ¡logo
            problemDetails.Extensions.Add("errorCode", Constants.OrderErrors.InternalError.Code);
            problemDetails.Extensions.Add("errorMessage", Constants.OrderErrors.InternalError.Message);
            
            string correlationId = "";
            if (httpContext.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            }
            problemDetails.Extensions.Add("correlationId", correlationId);

            httpContext.Items["ErrorCode"] = Constants.OrderErrors.InternalError.Code;
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // Siempre devolvemos true porque es el Ãºltimo handler de la cadena (red de seguridad)
            return true;
        }
    }
}

