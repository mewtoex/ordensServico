using Os.Api.Configuration;
using Os.Api.Infra;
using Os.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackend(builder.Configuration);
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseCors(CorsConfiguration.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBackendHealthChecks();
if (args.Contains("--seed-admin"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
    return;
}
app.Run();

public partial class Program { }
