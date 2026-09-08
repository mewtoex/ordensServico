namespace Os.Api.Configuration;

public static class CorsConfiguration
{
    public const string CorsPolicy = "Frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var normalizedOrigins = origins.Select(NormalizeOrigin).Distinct().ToArray();
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(normalizedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type")
            .WithExposedHeaders("Content-Disposition", "Location")));

        return services;
    }

    private static string NormalizeOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.Host.Contains('*') || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins deve conter apenas origens HTTP/HTTPS, sem caminhos ou curingas.");
        }
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
