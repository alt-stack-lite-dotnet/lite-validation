using Lite.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lite.Validation.Integration.AspNetCore.Mvc;

/// <summary>
/// Extension methods for MVC validation.
/// </summary>
public static class MvcValidationExtensions
{
    /// <summary>
    /// Adds validation errors from <paramref name="result"/> to <paramref name="modelState"/>.
    /// </summary>
    public static void AddValidationResult(this ModelStateDictionary modelState, ValidationResult result)
    {
        foreach (var error in result.Errors)
            modelState.AddModelError(error.PropertyName, error.Details);
    }
}
