using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers
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

            int statusCode = ex.StatusCode;

            string title;
            string rfcType;
            switch (statusCode)
            {
                case StatusCodes.Status401Unauthorized:
                    title = "No Autorizado";
                    rfcType = "https://tools.ietf.org/html/rfc7235#section-3.1";
                    break;
                case StatusCodes.Status403Forbidden:
                    title = "Prohibido";
                    rfcType = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                    break;
                case StatusCodes.Status422UnprocessableEntity:
                    title = "Entidad no procesable";
                    rfcType = "https://tools.ietf.org/html/rfc4918#section-11.2";
                    break;
                default:
                    title = "Conflicto de Negocio";
                    rfcType = "https://tools.ietf.org/html/rfc7231#section-6.5.9";
                    break;
            }

            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = statusCode;
            problemDetails.Title = title;
            problemDetails.Type = rfcType;
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

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

