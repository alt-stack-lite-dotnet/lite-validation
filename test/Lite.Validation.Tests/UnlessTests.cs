using System.Threading.Tasks;
using Lite.Validation.Fluent;
using Xunit;

namespace Lite.Validation.Tests;

public class UnlessTests
{
    private static ValidationBuilder<M> B() => new();

    private record M(string? Value, bool Skip);

    // ---------------------------------------------------------------------------
    // Unless (sync)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Unless_ConditionTrue_RuleSkipped()
    {
        var b = B();
        b.RuleFor(x => x.Value).NotNull().Unless(x => x.Skip);
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M(null, Skip: true));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Unless_ConditionFalse_RuleRuns()
    {
        var b = B();
        b.RuleFor(x => x.Value).NotNull().Unless(x => x.Skip);
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M(null, Skip: false));

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Unless_ValueValid_ReturnsSuccess()
    {
        var b = B();
        b.RuleFor(x => x.Value).NotNull().Unless(x => x.Skip);
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M("hello", Skip: false));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Unless_ChainedWithOtherRules_OnlyUnlessRuleSkipped()
    {
        var b = B();
        b.RuleFor(x => x.Value)
            .NotNull()
            .Must((_, v) => v != "bad").WithDetails("must not be bad").Unless(x => x.Skip);
        var v = new TestValidator<M>(b);

        // Skip=true → Unless skips "must not be bad", but NotNull still runs
        var result = v.Validate(new M("bad", Skip: true));
        Assert.True(result.IsSuccess);

        // Skip=false → both rules run
        var result2 = v.Validate(new M("bad", Skip: false));
        Assert.False(result2.IsSuccess);
    }

    // ---------------------------------------------------------------------------
    // UnlessAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task UnlessAsync_ConditionTrue_RuleSkipped()
    {
        var b = B();
        b.RuleFor(x => x.Value).NotNull().UnlessAsync(x => new System.Threading.Tasks.ValueTask<bool>(x.Skip));
        var v = new TestValidator<M>(b);

        var result = await v.ValidateAsync(new M(null, Skip: true));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UnlessAsync_ConditionFalse_RuleRuns()
    {
        var b = B();
        b.RuleFor(x => x.Value).NotNull().UnlessAsync(x => new System.Threading.Tasks.ValueTask<bool>(x.Skip));
        var v = new TestValidator<M>(b);

        var result = await v.ValidateAsync(new M(null, Skip: false));

        Assert.False(result.IsSuccess);
    }

    // ---------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------

    private class TestValidator<T> : LiteValidator<T>
    {
        public TestValidator(ValidationBuilder<T> builder) : base(builder) { }
    }
}
