using Orders.API.ExceptionHandlers;
using Orders.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Inicializar Logging (Serilog)
builder.AddAppLogging();

// Registrar HttpContextAccessor y el delegating handler para propagar Correlation ID en llamadas salientes
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

// Registro de Handlers en orden jerárquico (Paso a paso Persona A)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. ConfiguraciÃ³n de middlewares y rutas (incluyendo Health Checks)
app.UseAppMiddleware();

app.Run();
