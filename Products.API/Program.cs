using Products.API.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Método tradicional para configurar Serilog consumiendo appsettings.json
void ConfigurarSerilog(HostBuilderContext context, LoggerConfiguration configuration)
{
    configuration.ReadFrom.Configuration(context.Configuration);
}

builder.Host.UseSerilog(ConfigurarSerilog);

// 1. Registro de todos los servicios (DI)
builder.Services.AddAppServices();

var app = builder.Build();

// 2. Configuración de middlewares y rutas (incluyendo Health Checks)
app.UseAppMiddleware();

app.Run();
