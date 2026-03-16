using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lite.Benchmarks.AspNetCore;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class AspNetCoreValidationBenchmark
{
    private WebApplicationFactory<Lite.Benchmarks.AspNetCore.App.AppEntryPoint> _factory = null!;
    private HttpClient _client = null!;
    private static readonly CreateOrderRequestJson ValidPayload = new("Widget", 2, 9.99m);
    private static readonly CreateOrderRequestJson InvalidPayload = new("", 0, 0m);

    [GlobalSetup]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Lite.Benchmarks.AspNetCore.App.AppEntryPoint>();
        _client = _factory.CreateClient();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>POST /Order with valid body → 200.</summary>
    [Benchmark]
    public async Task<HttpResponseMessage> Post_Valid()
    {
        var response = await _client.PostAsJsonAsync("/Order", ValidPayload);
        return response;
    }

    /// <summary>POST /Order with invalid body → 400.</summary>
    [Benchmark]
    public async Task<HttpResponseMessage> Post_Invalid()
    {
        var response = await _client.PostAsJsonAsync("/Order", InvalidPayload);
        return response;
    }

    private sealed record CreateOrderRequestJson(string? ProductName, int Quantity, decimal Price);
}
