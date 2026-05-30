using Cart.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Inicializar Logging (Serilog)
builder.AddAppLogging();

// 1. Registro de todos los servicios (DI)
builder.Services.AddAppServices();

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

// 2. ConfiguraciÃ³n de middlewares y rutas (incluyendo Health Checks)
app.UseAppMiddleware();

app.Run();
