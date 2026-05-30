using Microsoft.Extensions.DependencyInjection;
using Products.API.ExceptionHandlers;
using Products.API.HealthChecks;
using Products.API.Data;

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

            // Registrar persistencia y base de datos
            services.AddSingleton<DatabaseInitializer>();
            services.AddScoped<ProductRepository>();

            // Registrar HttpClient para comunicación externa
            services.AddHttpClient();

            // Registrar Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                string xmlFile = typeof(ServicesExtensions).Assembly.GetName().Name + ".xml";
                string xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
                if (System.IO.File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            // Registrar Health Checks
            services.AddHealthChecks()
                .AddCheck<PersistencyHealthCheck>("persistency-check", null, new string[] { "database" })
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
