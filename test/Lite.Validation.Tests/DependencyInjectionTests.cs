using Lite.Validation;
using Lite.Validation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Validation.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var services = new ServiceCollection();
        services.AddLiteValidatorsFromAssemblyOf<DependencyInjectionTests>(lifetime);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddLiteValidators_RegistersIValidator_ForLiteValidatorSubclass()
    {
        using var provider = BuildProvider();
        var validator = provider.GetService<IValidator<CreateUserRequest>>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void AddLiteValidators_RegistersIAsyncValidator_ForLiteValidatorSubclass()
    {
        using var provider = BuildProvider();
        var validator = provider.GetService<IAsyncValidator<CreateUserRequest>>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void AddLiteValidators_RegistersIValidator_ForFluentValidatorSubclass()
    {
        // OrderFluentValidator : FluentValidator<CreateOrderRequest> (source-generated)
        using var provider = BuildProvider();
        var validator = provider.GetService<IValidator<CreateOrderRequest>>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void RegisteredLiteValidator_ValidatesCorrectly()
    {
        using var provider = BuildProvider();
        var v = provider.GetRequiredService<IValidator<CreateUserRequest>>();

        Assert.True(v.Validate(new CreateUserRequest("John", "john@test.com", 25)).IsSuccess);
        Assert.False(v.Validate(new CreateUserRequest(null, "bad", 25)).IsSuccess);
    }

    [Fact]
    public void RegisteredFluentValidator_ValidatesCorrectly()
    {
        using var provider = BuildProvider();
        var v = provider.GetRequiredService<IValidator<CreateOrderRequest>>();

        Assert.True(v.Validate(new CreateOrderRequest("Widget", 5, 9.99m)).IsSuccess);
        Assert.False(v.Validate(new CreateOrderRequest(null, 0, 0m)).IsSuccess);
    }

    [Fact]
    public void Transient_ReturnsNewInstanceEachTime()
    {
        using var provider = BuildProvider(ServiceLifetime.Transient);
        var v1 = provider.GetRequiredService<IValidator<CreateUserRequest>>();
        var v2 = provider.GetRequiredService<IValidator<CreateUserRequest>>();
        Assert.NotSame(v1, v2);
    }

    [Fact]
    public void Singleton_ReturnsSameInstance()
    {
        using var provider = BuildProvider(ServiceLifetime.Singleton);
        var v1 = provider.GetRequiredService<IValidator<CreateUserRequest>>();
        var v2 = provider.GetRequiredService<IValidator<CreateUserRequest>>();
        Assert.Same(v1, v2);
    }
}
