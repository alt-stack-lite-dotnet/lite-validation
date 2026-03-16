using System.Collections.Generic;
using FastEndpoints;
using Lite.Validation;
using FV = FluentValidation;

namespace Lite.Validation.Integration.AspNetCore.FastEndpoints;

/// <summary>
/// FastEndpoints validator adapter that delegates to a Lite.Validation validator.
/// </summary>
public class FluentValidator<TValidator, T> : Validator<T>
    where T : notnull
    where TValidator : IValidatorCore<T>
{
    private readonly TValidator _validator;

    public FluentValidator(TValidator validator) => _validator = validator;

    public override FV.Results.ValidationResult Validate(FV.ValidationContext<T> context)
    {
        if (_validator.IsAsync)
            throw new InvalidOperationException("Use ValidateAsync for async validators.");
        var result = (_validator as IValidator<T>)!.Validate(context.InstanceToValidate);
        return CreateResult(result);
    }

    public override async Task<FV.Results.ValidationResult> ValidateAsync(
        FV.ValidationContext<T> context,
        CancellationToken cancellationToken = default)
    {
        if (!_validator.IsAsync)
            return Validate(context);
        var result = await (_validator as IAsyncValidator<T>)!
            .ValidateAsync(context.InstanceToValidate, cancellationToken);
        return CreateResult(result);
    }

    private static FV.Results.ValidationResult CreateResult(ValidationResult result)
    {
        if (result.IsSuccess)
            return new FV.Results.ValidationResult();
        var failures = new List<FV.Results.ValidationFailure>(result.ErrorCount);
        foreach (var e in result.Errors)
            failures.Add(new FV.Results.ValidationFailure(e.PropertyName, e.Details));
        return new FV.Results.ValidationResult(failures);
    }
}
