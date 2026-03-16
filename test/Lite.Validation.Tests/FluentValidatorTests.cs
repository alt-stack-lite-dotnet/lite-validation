using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

// ---------------------------------------------------------------------------
// Validator definitions — generator reads these and produces sealed overrides
// ---------------------------------------------------------------------------

/// <summary>
/// No-dependency validator. Generator produces parameterless public ctor.
/// </summary>
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

/// <summary>
/// Validator with per-rule When/Unless conditions.
/// </summary>
public partial class OrderWithConditionsValidator : FluentValidator<CreateOrderRequest>
{
    static void Configure(ValidationBuilder<CreateOrderRequest> b)
    {
        b.RuleFor(x => x.ProductName)
            .NotNull().WithDetails("required");

        b.RuleFor(x => x.Quantity)
            .GreaterThan(0).WithDetails("positive")
            .Unless(x => x.Quantity == -1); // -1 is sentinel "skip"
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public class FluentValidatorTests
{
    // ------ OrderFluentValidator (no deps) ----------------------------------

    [Fact]
    public void NoDepValidator_ValidRequest_ReturnsSuccess()
    {
        var v = new OrderFluentValidator();
        var result = v.Validate(new CreateOrderRequest("Widget", 5, 9.99m));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void NoDepValidator_NullProductName_ReturnsError()
    {
        var v = new OrderFluentValidator();
        var result = v.Validate(new CreateOrderRequest(null, 5, 9.99m));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "ProductName" && e.Details == "Product name is required");
    }

    [Fact]
    public void NoDepValidator_ZeroQuantity_ReturnsError()
    {
        var v = new OrderFluentValidator();
        var result = v.Validate(new CreateOrderRequest("Widget", 0, 9.99m));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Quantity" && e.Details == "Quantity must be positive");
    }

    [Fact]
    public void NoDepValidator_MultipleErrors_ReturnsAll()
    {
        var v = new OrderFluentValidator();
        var result = v.Validate(new CreateOrderRequest(null, 0, 0m));
        Assert.False(result.IsSuccess);
        // null ProductName → NotNull + NotEmpty both fail = 2 errors; Quantity + Price = 2 more → 4 total
        Assert.Equal(4, result.Errors.Count);
    }

    [Fact]
    public void NoDepValidator_IsAsync_IsFalse()
    {
        var v = new OrderFluentValidator();
        Assert.False(v.IsAsync);
    }

    [Fact]
    public async Task NoDepValidator_ValidateAsync_WorksViaSyncWrap()
    {
        var v = new OrderFluentValidator();
        var result = await v.ValidateAsync(new CreateOrderRequest("X", 1, 1m));
        Assert.True(result.IsSuccess);
    }

    // ------ OrderWithConditionsValidator (Unless) ----------------------------

    [Fact]
    public void Unless_SentinelQuantity_SkipsQuantityRule()
    {
        var v = new OrderWithConditionsValidator();
        // Quantity = -1 is the sentinel → Unless fires → rule skipped → no error
        var result = v.Validate(new CreateOrderRequest("Widget", -1, 0m));
        // Result is success → no Quantity error (Unless skipped the rule)
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Unless_NormalQuantity_RunsRule()
    {
        var v = new OrderWithConditionsValidator();
        var result = v.Validate(new CreateOrderRequest("Widget", 0, 0m));
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Quantity" && e.Details == "positive");
    }
}
