using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

// Dependencies for inheritance chain
public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IDiscountService
{
    bool IsValid(string? code);
}

public class StubClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

public class StubDiscountService : IDiscountService
{
    public bool IsValid(string? code) => !string.IsNullOrEmpty(code);
}

// Base: one dependency
public partial class BaseOrderValidatorWithDep : FluentValidator<CreateOrderRequest>
{
    static void Configure(ValidationBuilder<CreateOrderRequest> b, IClock clock)
    {
        b.RuleFor(x => x.ProductName)
            .NotNull().WithDetails("Product name is required");
    }
}

// Derived: same dep + one more; generator should emit : base(clock) and both fields
public partial class DerivedOrderValidator : BaseOrderValidatorWithDep
{
    static void Configure(ValidationBuilder<CreateOrderRequest> b, IClock clock, IDiscountService discountService)
    {
        b.RuleFor(x => x.Quantity)
            .GreaterThan(0).WithDetails("Quantity must be positive");
        b.RuleFor(x => x.Price)
            .GreaterThan(0m).WithDetails("Price must be positive");
    }
}

public class ValidatorInheritanceTests
{
    [Fact]
    public void DerivedValidator_ConstructorTakesBaseAndOwnDeps()
    {
        var clock = new StubClock();
        var discount = new StubDiscountService();
        var v = new DerivedOrderValidator(clock, discount);
        Assert.NotNull(v);
    }

    [Fact]
    public void DerivedValidator_ValidRequest_Success()
    {
        var clock = new StubClock();
        var discount = new StubDiscountService();
        var v = new DerivedOrderValidator(clock, discount);
        var result = v.Validate(new CreateOrderRequest("Widget", 5, 9.99m));
        Assert.True(result.IsSuccess);
    }

    // Note: SG does not merge base rules into derived; inheritance is for ctor chaining only.
    // So DerivedOrderValidator only runs its own rules (Quantity, Price).

    [Fact]
    public void DerivedValidator_InvalidRequest_DerivedRuleFires()
    {
        var clock = new StubClock();
        var discount = new StubDiscountService();
        var v = new DerivedOrderValidator(clock, discount);
        var result = v.Validate(new CreateOrderRequest("Widget", 0, -1m));
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }
}
