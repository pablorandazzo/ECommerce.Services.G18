using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers
{
    public class NotFoundExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Verificamos si la excepciÃ³n es de tipo NotFoundException
            if (exception is NotFoundException == false)
            {
                return false;
            }

            // Convertimos al tipo especÃ­fico
            NotFoundException ex = (NotFoundException)exception;

            // Construimos la respuesta
            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = StatusCodes.Status404NotFound;
            problemDetails.Title = "Recurso no encontrado";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
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

            httpContext.Items["ErrorCode"] = ex.ErrorCode;
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

