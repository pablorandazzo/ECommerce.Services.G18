using Users.API.ExceptionHandlers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Registrar Serilog como el proveedor de logs por defecto
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Registro de Handlers en orden jerárquico (Paso a paso Persona B)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Mecanismo de Correlation ID y enriquecimiento de logs (Paso a paso Persona B)
app.Use(async (context, next) =>
{
    string headerKey = "X-Correlation-Id";
    string correlationId = "";

    // 1. Intentamos obtener la cabecera de la petición HTTP si existe
    if (context.Request.Headers.TryGetValue(headerKey, out var headerValue))
    {
        correlationId = headerValue.ToString();
    }

    // 2. Si no viene en la cabecera (o está vacía), generamos un nuevo ID
    if (string.IsNullOrEmpty(correlationId))
    {
        correlationId = System.Guid.NewGuid().ToString();
        
        // Lo guardamos en los Request Headers para que los Exception Handlers lo lean
        context.Request.Headers[headerKey] = correlationId;
    }

    // 3. Lo guardamos en Items del contexto para que esté disponible durante toda la petición
    context.Items["CorrelationId"] = correlationId;

    // 4. Lo agregamos a los Headers de la respuesta HTTP para que el cliente lo reciba
    context.Response.Headers[headerKey] = correlationId;

    // 5. Enriquecemos los logs dinámicamente usando LogContext de Serilog
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        using (Serilog.Context.LogContext.PushProperty("Endpoint", context.Request.Path.Value))
        {
            // Iniciamos el cronómetro para medir la duración de la petición
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            // Loggeamos el inicio de la petición
            Serilog.Log.Information("Inicio Request: {Method} {Path}", context.Request.Method, context.Request.Path);

            try
            {
                // Continuamos con el siguiente paso en la ejecución de la petición
                await next();
            }
            finally
            {
                // Detenemos el cronómetro y calculamos la duración
                stopwatch.Stop();
                double duracionMs = stopwatch.Elapsed.TotalMilliseconds;

                // Loggeamos el fin de la petición con su duración
                Serilog.Log.Information("Fin Request: {Method} {Path} - Duración: {Duration}ms", context.Request.Method, context.Request.Path, duracionMs);
            }
        }
    }
});

app.UseExceptionHandler(); // Middleware obligatorio para usar IExceptionHandler

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();
