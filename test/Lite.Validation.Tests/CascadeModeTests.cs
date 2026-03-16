using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

public class CascadeModeTests
{
    [Fact]
    public void RuleLevelCascadeMode_Stop_StopsOnFirstFailureForProperty()
    {
        var builder = new ValidationBuilder<CascadeModel>
        {
            RuleLevelCascadeMode = CascadeMode.Stop
        };
        builder.RuleFor(x => x.Value)
            .GreaterThan(0).WithDetails("must be > 0")
            .LessThan(10).WithDetails("must be < 10");

        var validator = new LiteValidator<CascadeModel>(builder);
        var result = validator.Validate(new CascadeModel(-5)); // fails both rules

        Assert.False(result.IsSuccess);
        // With Stop, only first failed rule adds an error for this property
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Errors, e => e.Details == "must be > 0");
    }

    [Fact]
    public void RuleLevelCascadeMode_Continue_AccumulatesAllRuleFailures()
    {
        var builder = new ValidationBuilder<CascadeModel>
        {
            RuleLevelCascadeMode = CascadeMode.Continue
        };
        builder.RuleFor(x => x.Value)
            .GreaterThan(0).WithDetails("must be > 0")
            .LessThan(10).WithDetails("must be < 10");

        var validator = new LiteValidator<CascadeModel>(builder);
        var result = validator.Validate(new CascadeModel(-5)); // fails both

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ErrorCount);
    }

    private record CascadeModel(int Value);
}
