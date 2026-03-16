using Lite.Validation.DependencyInjection;

namespace Lite.Benchmarks.AspNetCore.App;

/// <summary>Marker type for WebApplicationFactory (entry point must be non-static).</summary>
public sealed class AppEntryPoint { }

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddLiteValidatorsFromAssemblyOf<OrderFluentValidator>(ServiceLifetime.Singleton);
        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }
}
