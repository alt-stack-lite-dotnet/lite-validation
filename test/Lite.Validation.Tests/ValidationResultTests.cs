using Lite.Validation;

namespace Lite.Validation.Tests;

public class ValidationResultTests
{
    [Fact]
    public void Default_IsSuccess()
    {
        var result = new ValidationResult();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Add_MakesNotSuccess()
    {
        var result = new ValidationResult();
        result.Add("Prop", "Error");

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("Prop", result.Errors[0].PropertyName);
        Assert.Equal("Error", result.Errors[0].Details);
    }

    [Fact]
    public void MultipleErrors_AccumulatesAll()
    {
        var result = new ValidationResult();
        result.Add("A", "Error 1");
        result.Add("B", "Error 2");
        result.Add("C", "Error 3");

        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Errors_ThrowsWhenSuccess()
    {
        var result = new ValidationResult();
        Assert.Throws<InvalidOperationException>(() => result.Errors);
    }

    [Fact]
    public void AddRange_AddsAllErrors()
    {
        var result = new ValidationResult();
        result.AddRange(new[]
        {
            new ValidationError("A", "1"),
            new ValidationError("B", "2")
        });
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ErrorCount);
        Assert.Equal("A", result.Errors[0].PropertyName);
        Assert.Equal("1", result.Errors[0].Details);
    }

    [Fact]
    public void ErrorCount_ZeroWhenSuccess()
    {
        var result = new ValidationResult();
        Assert.Equal(0, result.ErrorCount);
        result.Add("X", "err");
        Assert.Equal(1, result.ErrorCount);
    }
}
