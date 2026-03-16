using System;
using System.Linq;
using System.Reflection;
using Lite.Validation;
using Lite.Validation.Fluent;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Validation.Integration.DependencyInjection;

/// <summary>
/// Microsoft.Extensions.DependencyInjection integration for Lite.Validation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the assembly containing <typeparamref name="TMarker"/> and registers all
    /// validators that implement <see cref="IValidator{T}"/> and/or <see cref="IAsyncValidator{T}"/>.
    /// </summary>
    public static IServiceCollection AddLiteValidatorsFromAssemblyOf<TMarker>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        var assembly = typeof(TMarker).Assembly;
        return services.AddLiteValidatorsFromAssembly(assembly, lifetime);
    }

    /// <summary>
    /// Scans the specified assembly and registers all validators that implement
    /// <see cref="IValidator{T}"/> and/or <see cref="IAsyncValidator{T}"/>.
    /// </summary>
    public static IServiceCollection AddLiteValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                continue;
            RegisterValidatorInterfaces(services, type, lifetime);
        }
        return services;
    }

    private static void RegisterValidatorInterfaces(IServiceCollection services, Type implementationType, ServiceLifetime lifetime)
    {
        foreach (var iface in implementationType.GetInterfaces().Where(i => i.IsGenericType))
        {
            var def = iface.GetGenericTypeDefinition();
            if (def != typeof(IValidator<>) && def != typeof(IAsyncValidator<>))
                continue;
            if (services.Any(d => d.ServiceType == iface && d.ImplementationType == implementationType))
                continue;
            var validatedType = iface.GetGenericArguments()[0];
            var factory = CreateValidatorFactory(implementationType, validatedType);
            services.Add(new ServiceDescriptor(iface, factory, lifetime));
        }
    }

    private static Func<IServiceProvider, object> CreateValidatorFactory(Type implementationType, Type validatedType)
    {
        var builderType = typeof(ValidationBuilder<>).MakeGenericType(validatedType);
        var ctors = implementationType.GetConstructors();
        if (ctors.Length == 0)
            return sp => throw new InvalidOperationException($"Validator {implementationType.Name} has no public constructor.");
        var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        return sp =>
        {
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.ParameterType == builderType)
                    args[i] = Activator.CreateInstance(builderType);
                else
                    args[i] = sp.GetService(p.ParameterType);
            }
            return ctor.Invoke(args);
        };
    }
}
