namespace Lite.Validation.Rules
{
    /// <summary>
    /// Rule that runs a nested validator and returns its errors.
    /// At compile time, delegates are bound so the runtime invokes Validate/ValidateAsync via CompiledRule.
    /// </summary>
    public class SetValidatorRule<T, TProperty> : IRule<T, TProperty>, IAsyncRule<T, TProperty>
    {
        private readonly IValidator<TProperty> _validator;

        public SetValidatorRule(IValidator<TProperty> validator)
        {
            _validator = validator;
        }

        public bool IsSatisfiedBy(T target, TProperty value)
            => !HasErrors(Validate(target, value));

        public async ValueTask<bool> IsSatisfiedByAsync(T target, TProperty value, CancellationToken cancellationToken)
            => !HasErrors(await ValidateAsync(target, value, cancellationToken).ConfigureAwait(false));

        public ValidationError[] Validate(T target, TProperty value)
        {
            if (value is null) return Array.Empty<ValidationError>();
            var result = _validator.Validate(value);
            return result.IsSuccess
                ? Array.Empty<ValidationError>()
                : result.Errors is ValidationError[] arr
                    ? arr
                    : result.Errors.ToArray();
        }

        public async ValueTask<ValidationError[]?> ValidateAsync(T target, TProperty value, CancellationToken cancellationToken)
        {
            if (value is null) return null;

            if (_validator is IAsyncValidator<TProperty> asyncValidator)
            {
                var res = await asyncValidator.ValidateAsync(value, cancellationToken);
                if (res.IsSuccess) return null;
                return res.Errors is ValidationError[] arr
                    ? arr
                    : res.Errors.ToArray();
            }

            var result = _validator.Validate(value);
            if (result.IsSuccess) return null;
            return result.Errors is ValidationError[] syncArr
                ? syncArr
                : result.Errors.ToArray();
        }

        private static bool HasErrors(ValidationError[]? errors)
        {
            if (errors is null)
                return false;

            return errors.Length > 0;
        }
    }
}
