using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Products.API.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Este handler captura TODO lo que llegue hasta aquí
            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Error Interno del Servidor";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            problemDetails.Detail = "Ocurrió un error inesperado. Por favor, contacte al administrador.";
            problemDetails.Instance = httpContext.Request.Path;

            // Usamos el código de error interno del catálogo
            problemDetails.Extensions.Add("errorCode", Constants.ProductErrors.InternalErrorCode);
            problemDetails.Extensions.Add("errorMessage", Constants.ProductErrors.InternalErrorMessage);
            
            string correlationId = "";
            if (httpContext.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            }
            problemDetails.Extensions.Add("correlationId", correlationId);

            httpContext.Items["ErrorCode"] = Constants.ProductErrors.InternalErrorCode;
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            // Siempre devolvemos true porque es el último handler de la cadena (red de seguridad)
            return true;
        }
    }
}
