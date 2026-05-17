using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers
{
    public class ValidationExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Verificamos si la excepciÃ³n es de tipo ValidationException
            if (exception is ValidationException == false)
            {
                return false; // No es de este tipo, que siga el siguiente handler
            }

            // Convertimos la excepciÃ³n al tipo especÃ­fico para leer sus datos
            ValidationException ex = (ValidationException)exception;

            // Creamos el objeto de respuesta de error (Problem Details)
            ProblemDetails problemDetails = new ProblemDetails();
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Error de Validación";
            problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            problemDetails.Detail = ex.Message;
            problemDetails.Instance = httpContext.Request.Path;

            // Agregamos los campos que pide el TP
            problemDetails.Extensions.Add("errorCode", ex.ErrorCode);
            problemDetails.Extensions.Add("errorMessage", ex.Message);
            
            // Buscamos el Correlation ID en los encabezados del request
            string correlationId = "";
            if (httpContext.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
            }
            problemDetails.Extensions.Add("correlationId", correlationId);

            // Configuramos la respuesta y la enviamos como JSON
            Serilog.Log.Warning("Error de negocio: {Message} - Código de error: {ErrorCode}", ex.Message, ex.ErrorCode);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; // Indicamos que el error ya fue manejado
        }
    }
}


