using System.Threading;
using Lite.Validation.Rules;

namespace Lite.Validation.Fluent.Runtime
{
    internal abstract class CompiledPropertyValidatorBase<TType>
    {
        public abstract bool HasAsyncRules { get; }
        public virtual int DependencyValidatorIndex => -1;
        public abstract void Validate(TType target, ref ValidationResult result);
        public abstract ValueTask<List<ValidationError>?> ValidateAsync(TType target, CancellationToken cancellationToken);
    }

    internal sealed class CompiledPropertyValidator<TType, TProperty> : CompiledPropertyValidatorBase<TType>
    {
        private readonly Func<TType, TProperty> _getter;
        private readonly string _propertyName;
        private readonly CompiledRule<TType, TProperty>[] _rules;
        private readonly CascadeMode _ruleLevelCascadeMode;
        private readonly Func<TType, bool>? _groupCondition;
        private readonly int _dependencyValidatorIndex;

        public CompiledPropertyValidator(
            Func<TType, TProperty> getter,
            string propertyName,
            CompiledRule<TType, TProperty>[] rules,
            CascadeMode ruleLevelCascadeMode,
            Func<TType, bool>? groupCondition = null,
            int dependencyValidatorIndex = -1)
        {
            _getter = getter;
            _propertyName = propertyName;
            _rules = rules;
            _ruleLevelCascadeMode = ruleLevelCascadeMode;
            _groupCondition = groupCondition;
            _dependencyValidatorIndex = dependencyValidatorIndex;
        }

        public override bool HasAsyncRules
        {
            get
            {
                foreach (var rule in _rules)
                {
                    if (rule.AsyncRule is not null || rule.AsyncCondition is not null)
                        return true;
                }
                return false;
            }
        }

        public override int DependencyValidatorIndex => _dependencyValidatorIndex;

        public override void Validate(TType target, ref ValidationResult result)
        {
            if (_groupCondition is not null && !_groupCondition(target))
                return;

            var value = _getter(target);

            foreach (var rule in _rules)
            {
                // Skip async rules in sync validation
                if (rule.AsyncRule is not null && rule.Rule is null)
                    continue;

                // Check sync condition
                if (rule.Condition is not null && !rule.Condition(target))
                    continue;

                // Nested validator (SetValidator) — returns multiple errors
                if (rule.GetErrors is { } getErrors)
                {
                    var errors = getErrors(target, value);
                    var length = errors.Length;
                    if (length > 0)
                    {
                        for (var i = 0; i < length; i++)
                        {
                            ref readonly var error = ref errors[i];
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? _propertyName
                                : $"{_propertyName}.{error.PropertyName}";
                            result.Add(propName, error.Details);
                        }

                        if (_ruleLevelCascadeMode == CascadeMode.Stop)
                            return;
                    }
                    continue;
                }

                // Standard sync rule
                if (rule.Rule is not null && !rule.Rule.IsSatisfiedBy(target, value))
                {
                    result.Add(_propertyName, rule.Details ?? "Validation failed");

                    // Stop: stop validating this property on first failure
                    if (_ruleLevelCascadeMode == CascadeMode.Stop)
                        return;
                }
            }
        }

        public override async ValueTask<List<ValidationError>?> ValidateAsync(TType target, CancellationToken cancellationToken)
        {
            if (_groupCondition is not null && !_groupCondition(target))
                return null;

            var value = _getter(target);
            List<ValidationError>? errors = null;

            foreach (var rule in _rules)
            {
                // Check condition (async or sync)
                if (rule.AsyncCondition is not null)
                {
                    if (!await rule.AsyncCondition(target).ConfigureAwait(false))
                        continue;
                }
                else if (rule.Condition is not null && !rule.Condition(target))
                {
                    continue;
                }

                // Nested validator (SetValidator) — async or sync
                if (rule.GetErrorsAsync is { } getErrorsAsync)
                {
                    var ruleErrors = await getErrorsAsync(target, value, cancellationToken).ConfigureAwait(false);
                    if (ruleErrors is not null && ruleErrors.Length > 0)
                    {
                        var arr = ruleErrors;
                        for (var i = 0; i < arr.Length; i++)
                        {
                            ref readonly var error = ref arr[i];
                            errors ??= new List<ValidationError>();
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? _propertyName
                                : $"{_propertyName}.{error.PropertyName}";
                            errors.Add(new ValidationError(propName, error.Details));
                        }
                        if (_ruleLevelCascadeMode == CascadeMode.Stop)
                            return errors;
                    }
                    continue;
                }
                if (rule.GetErrors is { } getErrorsSync)
                {
                    var ruleErrors = getErrorsSync(target, value);
                    if (ruleErrors.Length > 0)
                    {
                        errors ??= new List<ValidationError>();
                        for (var i = 0; i < ruleErrors.Length; i++)
                        {
                            ref readonly var error = ref ruleErrors[i];
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? _propertyName
                                : $"{_propertyName}.{error.PropertyName}";
                            errors.Add(new ValidationError(propName, error.Details));
                        }
                        if (_ruleLevelCascadeMode == CascadeMode.Stop)
                            return errors;
                    }
                    continue;
                }

                // Validate (async or sync)
                bool isValid;
                if (rule.AsyncRule is { } asyncRule)
                {
                    isValid = await asyncRule.IsSatisfiedByAsync(target, value, cancellationToken).ConfigureAwait(false);
                }
                else if (rule.Rule is { } syncRule)
                {
                    isValid = syncRule.IsSatisfiedBy(target, value);
                }
                else
                {
                    continue;
                }

                if (!isValid)
                {
                    errors ??= new List<ValidationError>();
                    errors.Add(new ValidationError(_propertyName, rule.Details ?? "Validation failed"));

                    // Stop: stop validating this property on first failure
                    if (_ruleLevelCascadeMode == CascadeMode.Stop)
                        return errors;
                }
            }

            return errors;
        }
    }

    internal readonly struct CompiledRule<TType, TProperty>
    {
        public IRule<TType, TProperty>? Rule { get; }
        public IAsyncRule<TType, TProperty>? AsyncRule { get; }
        public string? Details { get; }
        public Func<TType, bool>? Condition { get; }
        public Func<TType, ValueTask<bool>>? AsyncCondition { get; }
        /// <summary>When set (e.g. SetValidator), returns multiple errors; otherwise use Rule.IsSatisfiedBy.</summary>
        public Func<TType, TProperty, ValidationError[]>? GetErrors { get; }
        public Func<TType, TProperty, CancellationToken, ValueTask<ValidationError[]?>>? GetErrorsAsync { get; }

        public CompiledRule(
            IRule<TType, TProperty>? rule,
            IAsyncRule<TType, TProperty>? asyncRule,
            string? details,
            Func<TType, bool>? condition,
            Func<TType, ValueTask<bool>>? asyncCondition,
            Func<TType, TProperty, ValidationError[]>? getErrors = null,
            Func<TType, TProperty, CancellationToken, ValueTask<ValidationError[]?>>? getErrorsAsync = null)
        {
            Rule = rule;
            AsyncRule = asyncRule;
            Details = details;
            Condition = condition;
            AsyncCondition = asyncCondition;
            GetErrors = getErrors;
            GetErrorsAsync = getErrorsAsync;
        }
    }
}
