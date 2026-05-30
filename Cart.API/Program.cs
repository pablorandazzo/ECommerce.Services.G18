using Cart.API.ExceptionHandlers;
using Cart.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();

// Registrar HttpContextAccessor y el delegating handler para propagar Correlation ID en llamadas salientes
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Registro de Handlers en orden jerárquico (Paso a paso Persona B)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.

// Mecanismo de Correlation ID 
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

    // 5. Continuamos con el siguiente paso en la ejecución de la petición
    await next();
});

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.Run();
