using Lite.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lite.Validation.Integration.AspNetCore.Mvc;

/// <summary>
/// MVC action filter that validates request models. Use with [ServiceFilter(typeof(ValidationActionFilter{TRequest}))].
/// </summary>
public sealed class ValidationActionFilter<TModel> : IAsyncActionFilter
{
    private readonly IValidator<TModel>? _validator;
    private readonly IAsyncValidator<TModel>? _asyncValidator;

    public ValidationActionFilter(IValidatorCore<TModel> validator)
    {
        _validator = validator as IValidator<TModel>;
        _asyncValidator = validator as IAsyncValidator<TModel>;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.ActionArguments.Values.OfType<TModel>().FirstOrDefault();
        if (request is null || (_validator is null && _asyncValidator is null))
        {
            await next();
            return;
        }

        var validationResult = _validator?.Validate(request);
        if (validationResult is null)
            validationResult = await _asyncValidator!.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (validationResult is not { } result)
        {
            await next();
            return;
        }

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.Details);
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return;
        }

        await next();
    }
}
