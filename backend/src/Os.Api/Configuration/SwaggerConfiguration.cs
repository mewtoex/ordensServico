using Microsoft.OpenApi.Models;

namespace Os.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Faça login em /api/auth/login e informe o accessToken."
            });
            options.OperationFilter<BearerSecurityOperationFilter>();
        });
        return services;
    }
}
