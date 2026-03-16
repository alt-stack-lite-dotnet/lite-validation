using System;
using Lite.Validation.Fluent;
using Xunit;

namespace Lite.Validation.Tests;

public class TransformTests
{
    private static ValidationBuilder<M> B() => new();

    private record M(string? Text, int Number);

    // ---------------------------------------------------------------------------
    // Same-type transform
    // ---------------------------------------------------------------------------

    [Fact]
    public void Transform_SameType_AppliesTransformBeforeValidation()
    {
        var b = B();
        // Trim whitespace before checking NotEmpty
        b.Transform(x => x.Text, v => v?.Trim()).NotEmpty().WithDetails("Must not be empty");
        var v = new TestValidator<M>(b);

        // "   " trimmed to "" → fails NotEmpty
        var fail = v.Validate(new M("   ", 0));
        Assert.False(fail.IsSuccess);
        Assert.Equal("Text", fail.Errors[0].PropertyName);

        // "hello" → passes
        var pass = v.Validate(new M("hello", 0));
        Assert.True(pass.IsSuccess);
    }

    [Fact]
    public void Transform_NullInput_NullPassedToRules()
    {
        var b = B();
        b.Transform(x => x.Text, v => v?.ToUpper()).NotNull().WithDetails("Required");
        var v = new TestValidator<M>(b);

        // null → ToUpper not called (null-safe), null fails NotNull
        var result = v.Validate(new M(null, 0));
        Assert.False(result.IsSuccess);
    }

    // ---------------------------------------------------------------------------
    // Different-type transform (string → int)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Transform_DifferentType_ChangesPropertyType()
    {
        var b = B();
        // Transform string → int (length), then validate int
        b.Transform(x => x.Text, v => v?.Length ?? 0).Must((_, len) => len >= 3).WithDetails("Too short");
        var v = new TestValidator<M>(b);

        var fail = v.Validate(new M("ab", 0));
        Assert.False(fail.IsSuccess);
        Assert.Equal("Text", fail.Errors[0].PropertyName);

        var pass = v.Validate(new M("abc", 0));
        Assert.True(pass.IsSuccess);
    }

    [Fact]
    public void Transform_MultipleRulesOnTransformed_AllRun()
    {
        var b = B();
        b.Transform(x => x.Text, v => v?.Trim() ?? "")
            .NotEmpty().WithDetails("required")
            .Must((_, s) => s.Length <= 10).WithDetails("too long");
        var v = new TestValidator<M>(b);

        // empty → first rule fails
        var r1 = v.Validate(new M("", 0));
        Assert.False(r1.IsSuccess);
        Assert.Equal("required", r1.Errors[0].Details);

        // too long → second rule fails
        var r2 = v.Validate(new M("12345678901", 0));
        Assert.False(r2.IsSuccess);
        Assert.Equal("too long", r2.Errors[0].Details);
    }

    // ---------------------------------------------------------------------------
    // Property name is preserved from source expression
    // ---------------------------------------------------------------------------

    [Fact]
    public void Transform_ErrorUsesSourcePropertyName()
    {
        var b = B();
        b.Transform(x => x.Number, v => v - 1).GreaterThan(0).WithDetails("Must be positive after transform");
        var v = new TestValidator<M>(b);

        var result = v.Validate(new M(null, 1)); // 1 - 1 = 0, not > 0
        Assert.False(result.IsSuccess);
        Assert.Equal("Number", result.Errors[0].PropertyName);
    }

    // ---------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------

    private class TestValidator<T> : LiteValidator<T>
    {
        public TestValidator(ValidationBuilder<T> builder) : base(builder) { }
    }
}
