using System;
using System.Linq;
using System.Reflection;
using Lite.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Validation.DependencyInjection;

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
            services.Add(new ServiceDescriptor(iface, implementationType, lifetime));
        }
    }
}
