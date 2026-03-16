namespace Lite.Benchmarks.AspNetCore.App;

public record CreateOrderRequest(string? ProductName, int Quantity, decimal Price);
