using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lite.Validation.Rules;

namespace Lite.Validation.Fluent.Runtime
{
    internal sealed class CompiledCollectionPropertyValidator<TType, TElement> : CompiledPropertyValidatorBase<TType>
    {
        private readonly Func<TType, IEnumerable<TElement>> _getter;
        private readonly string _propertyName;
        private readonly CompiledRule<TType, TElement>[] _rules;
        private readonly CascadeMode _ruleLevelCascadeMode;
        private readonly Func<TType, bool>? _groupCondition;
        private readonly int _dependencyValidatorIndex;

        public CompiledCollectionPropertyValidator(
            Func<TType, IEnumerable<TElement>> getter,
            string propertyName,
            CompiledRule<TType, TElement>[] rules,
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

        public override int DependencyValidatorIndex => _dependencyValidatorIndex;

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

        public override void Validate(TType target, ref ValidationResult result)
        {
            if (_groupCondition is not null && !_groupCondition(target))
                return;

            var collection = _getter(target);
            if (collection is null) return; // RuleFor(x).NotNull() should handle null collection itself. RuleForEach skips null.

            int index = 0;
            foreach (var element in collection)
            {
                ValidateElement(target, element, index++, ref result);
            }
        }

        private void ValidateElement(TType target, TElement element, int index, ref ValidationResult result)
        {
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
                    var ruleErrors = getErrors(target, element);
                    if (ruleErrors.Length > 0)
                    {
                        for (var i = 0; i < ruleErrors.Length; i++)
                        {
                            ref readonly var error = ref ruleErrors[i];
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? $"{_propertyName}[{index}]"
                                : $"{_propertyName}[{index}].{error.PropertyName}";
                            result.Add(propName, error.Details);
                        }
                        if (_ruleLevelCascadeMode == CascadeMode.Stop)
                            return;
                    }
                    continue;
                }

                // Simple rule
                if (rule.Rule is not null && !rule.Rule.IsSatisfiedBy(target, element))
                {
                    string key = $"{_propertyName}[{index}]";
                    result.Add(key, rule.Details ?? "Validation failed");

                    if (_ruleLevelCascadeMode == CascadeMode.Stop)
                        return;
                }
            }
        }

        public override async ValueTask<List<ValidationError>?> ValidateAsync(TType target, CancellationToken cancellationToken)
        {
            if (_groupCondition is not null && !_groupCondition(target))
                return null;

            var collection = _getter(target);
            if (collection is null) return null;

            List<ValidationError>? allErrors = null;
            int index = 0;

            foreach (var element in collection)
            {
                var elementErrors = await ValidateElementAsync(target, element, index++, cancellationToken).ConfigureAwait(false);
                if (elementErrors is not null)
                {
                    allErrors ??= new List<ValidationError>();
                    allErrors.AddRange(elementErrors);
                }
            }

            return allErrors;
        }

        private async ValueTask<List<ValidationError>?> ValidateElementAsync(TType target, TElement element, int index, CancellationToken cancellationToken)
        {
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
                    var ruleErrors = await getErrorsAsync(target, element, cancellationToken).ConfigureAwait(false);
                    if (ruleErrors is not null && ruleErrors.Length > 0)
                    {
                        for (var i = 0; i < ruleErrors.Length; i++)
                        {
                            ref readonly var error = ref ruleErrors[i];
                            errors ??= new List<ValidationError>();
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? $"{_propertyName}[{index}]"
                                : $"{_propertyName}[{index}].{error.PropertyName}";
                            errors.Add(new ValidationError(propName, error.Details));
                        }
                        if (_ruleLevelCascadeMode == CascadeMode.Stop)
                            return errors;
                    }
                    continue;
                }
                if (rule.GetErrors is { } getErrorsSync)
                {
                    var ruleErrors = getErrorsSync(target, element);
                    if (ruleErrors.Length > 0)
                    {
                        errors ??= new List<ValidationError>();
                        for (var i = 0; i < ruleErrors.Length; i++)
                        {
                            ref readonly var error = ref ruleErrors[i];
                            var propName = string.IsNullOrEmpty(error.PropertyName)
                                ? $"{_propertyName}[{index}]"
                                : $"{_propertyName}[{index}].{error.PropertyName}";
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
                    isValid = await asyncRule.IsSatisfiedByAsync(target, element, cancellationToken).ConfigureAwait(false);
                }
                else if (rule.Rule is { } syncRule)
                {
                    isValid = syncRule.IsSatisfiedBy(target, element);
                }
                else
                {
                    continue;
                }

                if (!isValid)
                {
                    string key = $"{_propertyName}[{index}]";
                    errors ??= new List<ValidationError>();
                    errors.Add(new ValidationError(key, rule.Details ?? "Validation failed"));

                    // Stop: stop validating this element on first failure
                    if (_ruleLevelCascadeMode == CascadeMode.Stop)
                        return errors;
                }
            }
            return errors;
        }
    }
}
