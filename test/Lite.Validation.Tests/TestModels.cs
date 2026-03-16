namespace Lite.Validation.Tests;

public record CreateUserRequest(
    string? Name,
    string? Email,
    int Age);

public record CreateOrderRequest(
    string? ProductName,
    int Quantity,
    decimal Price);
