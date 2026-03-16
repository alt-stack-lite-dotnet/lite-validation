using FluentValidation;
using Lite.Validation;
using Lite.Validation.Integration.AspNetCore.FastEndpoints;
using Xunit;

namespace Lite.Validation.Integration.AspNetCore.Test;

public class FastEndpointsIntegrationTests
{
    [Fact]
    public void FluentValidator_ValidRequest_ReturnsSuccess()
    {
        var inner = new TestRequestValidator();
        var adapter = new FluentValidator<TestRequestValidator, TestRequest>(inner);
        var context = new ValidationContext<TestRequest>(new TestRequest("ok", 5));

        var result = adapter.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FluentValidator_InvalidRequest_ReturnsErrors()
    {
        var inner = new TestRequestValidator();
        var adapter = new FluentValidator<TestRequestValidator, TestRequest>(inner);
        var context = new ValidationContext<TestRequest>(new TestRequest("", 0));

        var result = adapter.Validate(context);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Age");
    }
}
