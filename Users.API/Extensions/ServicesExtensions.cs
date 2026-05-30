using Microsoft.Extensions.DependencyInjection;
using Users.API.ExceptionHandlers;
using Users.API.HealthChecks;

namespace Users.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            // Registrar ProblemDetails
            services.AddProblemDetails();

            // Registro de Handlers en orden jerárquico (Paso a paso Persona B)
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
                setup.AddHealthCheckEndpoint("Users.API", "/health"); // Endpoint local
            }).AddInMemoryStorage();
        }
    }
}
