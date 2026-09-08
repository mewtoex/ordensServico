using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Os.Api.Application;
using Xunit;

namespace Os.Tests;

public class CatalogValidationTests
{
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void PriceLimitsDoNotDependOnCurrentCulture(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var priceParameter = typeof(CatalogRequest).GetConstructors().Single().GetParameters()
                .Single(parameter => parameter.Name == "Price");
            var range = priceParameter.GetCustomAttribute<RangeAttribute>()!;
            Assert.True(range.IsValid(0.01m));
            Assert.True(range.IsValid(125.50m));
            Assert.False(range.IsValid(0m));
            Assert.False(range.IsValid(100000000m));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
