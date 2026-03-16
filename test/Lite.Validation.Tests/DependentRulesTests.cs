using Lite.Validation.Fluent;
using Xunit;

namespace Lite.Validation.Tests;

public class DependentRulesTests
{
    private record M(string? Email, string? Username, int Age);

    // ---------------------------------------------------------------------------
    // Basic: parent passes → dependent runs
    // ---------------------------------------------------------------------------

    [Fact]
    public void DependentRules_ParentPasses_DependentRuns()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Email).Must((_, v) => v!.Contains("@")).WithDetails("Email invalid");
            });
        var v = new TestValidator<M>(b);

        // Email not null (parent passes) → dependent runs → fails format check
        var result = v.Validate(new M("notanemail", "user", 25));
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Email invalid", result.Errors[0].Details);
    }

    [Fact]
    public void DependentRules_ParentPasses_DependentPassesToo()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Email).Must((_, v) => v!.Contains("@")).WithDetails("Email invalid");
            });
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M("user@example.com", "user", 25));
        Assert.True(result.IsSuccess);
    }

    // ---------------------------------------------------------------------------
    // Basic: parent fails → dependent skipped
    // ---------------------------------------------------------------------------

    [Fact]
    public void DependentRules_ParentFails_DependentSkipped()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Email).Must((_, v) => v!.Contains("@")).WithDetails("Email invalid");
            });
        var v = new TestValidator<M>(b);

        // Email is null → parent fails → dependent skipped → only 1 error
        var result = v.Validate(new M(null, "user", 25));
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Email null", result.Errors[0].Details);
    }

    // ---------------------------------------------------------------------------
    // Cross-property: dependent on different property
    // ---------------------------------------------------------------------------

    [Fact]
    public void DependentRules_CrossProperty_DependentOnDifferentProp()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                // Username only validated if Email is present
                b.RuleFor(x => x.Username).NotNull().WithDetails("Username null");
            });
        var v = new TestValidator<M>(b);

        // Email null → dependent (Username) skipped
        var r1 = v.Validate(new M(null, null, 25));
        Assert.Single(r1.Errors);
        Assert.Equal("Email null", r1.Errors[0].Details);

        // Email valid, Username null → both fail? No: only Username fails (Email passes)
        var r2 = v.Validate(new M("a@b.c", null, 25));
        Assert.Single(r2.Errors);
        Assert.Equal("Username null", r2.Errors[0].Details);
    }

    // ---------------------------------------------------------------------------
    // Multiple dependents
    // ---------------------------------------------------------------------------

    [Fact]
    public void DependentRules_MultipleDependents_AllSkippedWhenParentFails()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Email).Must((_, v) => v!.Contains("@")).WithDetails("Email format");
                b.RuleFor(x => x.Username).NotNull().WithDetails("Username null");
                b.RuleFor(x => x.Age).GreaterThan(0).WithDetails("Age invalid");
            });
        var v = new TestValidator<M>(b);

        // Email null → only 1 error (the parent), all 3 dependents skipped
        var result = v.Validate(new M(null, null, -1));
        Assert.Single(result.Errors);
        Assert.Equal("Email null", result.Errors[0].Details);
    }

    [Fact]
    public void DependentRules_MultipleDependents_AllRunWhenParentPasses()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Email).Must((_, v) => v!.Contains("@")).WithDetails("Email format");
                b.RuleFor(x => x.Username).NotNull().WithDetails("Username null");
            });
        var v = new TestValidator<M>(b);

        // Email not null → both dependents run and fail
        var result = v.Validate(new M("notanemail", null, 25));
        Assert.Equal(2, result.Errors.Count);
    }

    // ---------------------------------------------------------------------------
    // Rules outside DependentRules always run
    // ---------------------------------------------------------------------------

    [Fact]
    public void DependentRules_IndependentRulesAlwaysRun()
    {
        var b = new ValidationBuilder<M>();
        b.RuleFor(x => x.Age).GreaterThan(0).WithDetails("Age invalid");
        b.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email null")
            .DependentRules(() =>
            {
                b.RuleFor(x => x.Username).NotNull().WithDetails("Username null");
            });
        var v = new TestValidator<M>(b);

        // Age fails, Email null (parent fails → Username skipped) → 2 errors: Age + Email
        var result = v.Validate(new M(null, null, -1));
        Assert.Equal(2, result.Errors.Count);
    }

    // ---------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------

    private class TestValidator<T> : LiteValidator<T>
    {
        public TestValidator(ValidationBuilder<T> builder) : base(builder) { }
    }
}
