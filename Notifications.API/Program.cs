using Notifications.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Inicializar Logging (Serilog)
builder.AddAppLogging();

// 1. Registro de todos los servicios (DI)
builder.Services.AddAppServices();

var app = builder.Build();

// 2. ConfiguraciÃ³n de middlewares y rutas (incluyendo Health Checks)
app.UseAppMiddleware();

app.Run();
