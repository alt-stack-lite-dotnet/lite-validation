using Lite.Validation.Fluent;

namespace Lite.Benchmarks.AspNetCore.App;

public partial class OrderFluentValidator : FluentValidator<CreateOrderRequest>
{
    static void Configure(ValidationBuilder<CreateOrderRequest> b)
    {
        b.RuleFor(x => x.ProductName)
            .NotNull().WithDetails("Product name is required")
            .NotEmpty().WithDetails("Product name must not be empty");
        b.RuleFor(x => x.Quantity)
            .GreaterThan(0).WithDetails("Quantity must be positive");
        b.RuleFor(x => x.Price)
            .GreaterThan(0m).WithDetails("Price must be positive");
    }
}
