using Microsoft.Extensions.DependencyInjection;
using Orders.API.ExceptionHandlers;
using Orders.API.HealthChecks;

namespace Orders.API.Extensions
{
    public static class ServicesExtensions
    {
        // MÃ©todo de extensiÃ³n para registrar todos los servicios en el contenedor de dependencias (DI)
        public static void AddAppServices(this IServiceCollection services)
        {
            // Habilitamos Problem Details para el manejo estandarizado de errores
            services.AddProblemDetails();

            // Registro de los manejadores globales de excepciones (Exception Handlers)
            // Se evalÃºan en el orden en que se registran
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<NotFoundExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            // ConfiguraciÃ³n bÃ¡sica para Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // 1. Registro de Health Checks (Monitoreo de estado)
            services.AddHealthChecks()
                .AddCheck<PersistencyHealthCheck>("persistency-check", tags: new[] { "database" })
                .AddCheck<ApiStatusCheck>("api-status", tags: new[] { "api" });

            // 2. Registro del panel visual (Dashboard UI) y almacenamiento en memoria
            services.AddHealthChecksUI(setup =>
            {
                // Evaluamos el estado cada 10 minutos (600 segundos) para no sobrecargar el sistema
                setup.SetEvaluationTimeInSeconds(600);
                
                // Agregamos la ruta local de nuestra API para que el panel la consuma
                setup.AddHealthCheckEndpoint("Products API - Monitoreo", "/health");
            }).AddInMemoryStorage();
        }
    }
}

