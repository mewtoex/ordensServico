using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Os.Api.Configuration;
using Os.Api.Domain;
using Xunit;

namespace Os.Tests;

public class ArchitectureTests
{
    [Fact]
    public void ControllersDependOnlyOnServiceInterfaces()
    {
        var controllers = typeof(ServiceOrder).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
        Assert.All(controllers, controller => Assert.All(controller.GetConstructors().SelectMany(ctor => ctor.GetParameters()), parameter =>
        {
            Assert.True(parameter.ParameterType.IsInterface);
            Assert.Equal("Os.Api.Application.Interfaces.Services", parameter.ParameterType.Namespace);
        }));
    }

    [Fact]
    public void ServicesAndRepositoriesAreRegisteredWithTheirOwnInterfaces()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-signing-key-with-at-least-32-bytes",
            ["ConnectionStrings:Database"] = "Server=localhost;Database=ArchitectureTest;Integrated Security=true"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddBackend(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var implementations = typeof(ServiceOrder).Assembly.GetTypes().Where(type => type.IsPublic && type.IsClass && !type.IsAbstract
            && (type.Namespace == "Os.Api.Application.Services" || type.Namespace == "Os.Api.Infra.Repositories"));

        Assert.All(implementations, implementation =>
        {
            var contract = Assert.Single(implementation.GetInterfaces(), type => type.Name == "I" + implementation.Name);
            Assert.IsType(implementation, scope.ServiceProvider.GetRequiredService(contract));
            Assert.All(implementation.GetConstructors().SelectMany(ctor => ctor.GetParameters()), parameter =>
            {
                if (implementation.Namespace == "Os.Api.Application.Services")
                {
                    Assert.True(parameter.ParameterType.IsInterface);
                }
            });
        });
    }

    [Fact]
    public void ServiceContractsDoNotExposeDomainEntitiesOrQueryable()
    {
        var contracts = typeof(ServiceOrder).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "Os.Api.Application.Interfaces.Services");
        Assert.All(contracts.SelectMany(type => type.GetMethods()), method => CheckBoundary(method.ReturnType));
    }

    private static void CheckBoundary(Type type)
    {
        Assert.False(type.IsClass && type.Namespace == "Os.Api.Domain", $"Domain entity exposed: {type.Name}");
        Assert.False(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));
        foreach (var argument in type.GetGenericArguments())
        {
            CheckBoundary(argument);
        }
    }
}
