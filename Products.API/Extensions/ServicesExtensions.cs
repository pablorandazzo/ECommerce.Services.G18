using Microsoft.Extensions.DependencyInjection;
using Products.API.ExceptionHandlers;
using Products.API.HealthChecks;

namespace Products.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            // Registrar ProblemDetails
            services.AddProblemDetails();

            // Registro de Handlers en orden jerárquico (Paso a paso Persona A)
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<NotFoundExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            // Registrar Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Registrar Health Checks
            services.AddHealthChecks()
                .AddCheck<ApiStatusCheck>("api-status", null, new string[] { "api" });

            // Registrar Health Checks UI
            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(60); // Tiempo de re-evaluación
                setup.AddHealthCheckEndpoint("Products.API", "/health"); // Endpoint local
            }).AddInMemoryStorage();
        }
    }
}
