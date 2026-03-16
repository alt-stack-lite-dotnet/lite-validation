using Lite.Validation.Fluent;
using Xunit;

namespace Lite.Validation.Tests;

public class WhenBlockTests
{
    private static ValidationBuilder<M> B() => new();

    private record M(string? Name, int Age, bool IsAdmin, string? AdminCode);

    // ---------------------------------------------------------------------------
    // Basic When-block
    // ---------------------------------------------------------------------------

    [Fact]
    public void When_ConditionFalse_AllBlockRulesSkipped()
    {
        var b = B();
        b.When(x => x.IsAdmin, () =>
        {
            b.RuleFor(x => x.AdminCode).NotNull().WithDetails("AdminCode required");
        });
        var v = new TestValidator<M>(b);

        // Not admin → AdminCode check skipped
        var result = v.Validate(new M("Alice", 30, IsAdmin: false, AdminCode: null));
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void When_ConditionTrue_BlockRulesRun()
    {
        var b = B();
        b.When(x => x.IsAdmin, () =>
        {
            b.RuleFor(x => x.AdminCode).NotNull().WithDetails("AdminCode required");
        });
        var v = new TestValidator<M>(b);

        // Is admin, no AdminCode → fails
        var result = v.Validate(new M("Alice", 30, IsAdmin: true, AdminCode: null));
        Assert.False(result.IsSuccess);
        Assert.Equal("AdminCode required", result.Errors[0].Details);
    }

    [Fact]
    public void When_ConditionTrue_BlockRulesPassOnValidData()
    {
        var b = B();
        b.When(x => x.IsAdmin, () =>
        {
            b.RuleFor(x => x.AdminCode).NotNull().WithDetails("AdminCode required");
        });
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M("Alice", 30, IsAdmin: true, AdminCode: "ABC123"));
        Assert.True(result.IsSuccess);
    }

    // ---------------------------------------------------------------------------
    // Multiple properties in one When block
    // ---------------------------------------------------------------------------

    [Fact]
    public void When_MultiplePropertiesInBlock_AllSkippedOrAllRun()
    {
        var b = B();
        b.When(x => x.IsAdmin, () =>
        {
            b.RuleFor(x => x.AdminCode).NotNull().WithDetails("code required");
            b.RuleFor(x => x.Age).GreaterThan(18).WithDetails("must be adult admin");
        });
        var v = new TestValidator<M>(b);

        // Condition false → 0 errors
        var r1 = v.Validate(new M("A", 10, false, null));
        Assert.True(r1.IsSuccess);

        // Condition true, both fail → 2 errors
        var r2 = v.Validate(new M("A", 10, true, null));
        Assert.False(r2.IsSuccess);
        Assert.Equal(2, r2.Errors.Count);
    }

    // ---------------------------------------------------------------------------
    // Rules outside When block always run
    // ---------------------------------------------------------------------------

    [Fact]
    public void When_RulesOutsideBlock_AlwaysRun()
    {
        var b = B();
        b.RuleFor(x => x.Name).NotNull().WithDetails("Name always required");
        b.When(x => x.IsAdmin, () =>
        {
            b.RuleFor(x => x.AdminCode).NotNull().WithDetails("AdminCode required for admin");
        });
        var v = new TestValidator<M>(b);

        // Not admin, no name → Name error only
        var result = v.Validate(new M(null, 30, false, null));
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Name always required", result.Errors[0].Details);
    }

    // ---------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------

    private class TestValidator<T> : LiteValidator<T>
    {
        public TestValidator(ValidationBuilder<T> builder) : base(builder) { }
    }
}
