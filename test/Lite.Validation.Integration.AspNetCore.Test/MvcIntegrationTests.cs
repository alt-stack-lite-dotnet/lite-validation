using System.Collections.Generic;
using System.Threading.Tasks;
using Lite.Validation;
using Lite.Validation.Integration.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Lite.Validation.Integration.AspNetCore.Test;

public class MvcIntegrationTests
{
    [Fact]
    public async Task ValidationActionFilter_ValidModel_ProceedsToNext()
    {
        var validator = new TestRequestValidator();
        var filter = new ValidationActionFilter<TestRequest>(validator);
        var request = new TestRequest("ok", 1);
        var context = CreateContext(request);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller)
            {
                Result = context.Result
            });
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task ValidationActionFilter_InvalidModel_Returns400AndAddsModelErrors()
    {
        var validator = new TestRequestValidator();
        var filter = new ValidationActionFilter<TestRequest>(validator);
        var request = new TestRequest("", 0); // invalid
        var context = CreateContext(request);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller)
            {
                Result = context.Result
            });
        });

        Assert.False(nextCalled);
        Assert.NotNull(context.Result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.NotNull(context.ModelState);
        Assert.True(context.ModelState.ErrorCount > 0);
        Assert.IsType<ValidationProblemDetails>(badRequest.Value);
    }

    [Fact]
    public void AddValidationResult_AddsErrorsToModelState()
    {
        var modelState = new ModelStateDictionary();
        var result = new ValidationResult();
        result.Add("Name", "Required");
        result.Add("Age", "Must be positive");

        modelState.AddValidationResult(result);

        Assert.Equal(2, modelState.ErrorCount);
        Assert.True(modelState.ContainsKey("Name"));
        Assert.True(modelState.ContainsKey("Age"));
    }

    private static ActionExecutingContext CreateContext(TestRequest request)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["request"] = request },
            null!);
        return context;
    }
}

public record TestRequest(string Name, int Age);

public class TestRequestValidator : IValidator<TestRequest>, IValidatorCore<TestRequest>
{
    public bool IsAsync => false;

    public ValidationResult Validate(TestRequest target)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(target.Name))
            result.Add("Name", "Name is required");
        if (target.Age <= 0)
            result.Add("Age", "Age must be positive");
        return result;
    }
}
