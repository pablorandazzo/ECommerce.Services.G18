using Users.API.ExceptionHandlers;
var builder = WebApplication.CreateBuilder(args);

// 1. Configuraciones de Servicios (DI Container)
builder.Services.AddProblemDetails();

// Registro de Handlers en orden jerárquico (Paso a paso Persona B)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Configuraciones del Pipeline de Middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- ACÃ IRÃN TUS FUTUROS MAPEO DE ENDPOINTS ---
app.Run();


