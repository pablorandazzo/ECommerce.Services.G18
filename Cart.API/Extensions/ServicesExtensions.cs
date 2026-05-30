using Microsoft.Extensions.DependencyInjection;
using Cart.API.ExceptionHandlers;
using Cart.API.HealthChecks;
using Cart.API.Infrastructure;

namespace Cart.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services)
        {
            // Registrar ProblemDetails
            services.AddProblemDetails();

            // Registrar HttpContextAccessor y el delegating handler para propagar Correlation ID en llamadas salientes
            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationIdDelegatingHandler>();

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
                setup.AddHealthCheckEndpoint("Cart.API", "/health"); // Endpoint local
            }).AddInMemoryStorage();
        }
    }
}
