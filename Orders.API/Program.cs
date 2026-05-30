using Microsoft.AspNetCore.Builder;
using Orders.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Inicializar Logging (Serilog)
builder.AddAppLogging();

// 2. Registrar Servicios (ProblemDetails, Swagger, HealthChecks, etc.)
builder.Services.AddAppServices();

var app = builder.Build();

// 3. Configurar Middleware y Endpoints
app.UseAppMiddleware();

app.Run();
