using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers
{
    public class BusinessRuleExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Verificamos si es un error de regla de negocio
            if (exception is BusinessRuleException == false)
            {
                return false;
            }

            BusinessRuleException ex = (BusinessRuleException)exception;

            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = StatusCodes.Status409Conflict;
            problemDetails.Title = "Conflicto de Negocio";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9";
            problemDetails.Detail = ex.Message;
            problemDetails.Instance = httpContext.Request.Path;

            problemDetails.Extensions.Add("errorCode", ex.ErrorCode);
            problemDetails.Extensions.Add("errorMessage", ex.Message);
            
            string correlationId = "";
            if (httpContext.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            }
            problemDetails.Extensions.Add("correlationId", correlationId);

            Serilog.Log.Warning("Error de negocio: {Message} - Código de error: {ErrorCode}", ex.Message, ex.ErrorCode);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}


