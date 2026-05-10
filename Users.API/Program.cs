var builder = WebApplication.CreateBuilder(args);

// 1. Configuraciones de Servicios (DI Container)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Configuraciones del Pipeline de Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- ACÁ IRÁN TUS FUTUROS MAPEO DE ENDPOINTS ---
app.Run();

