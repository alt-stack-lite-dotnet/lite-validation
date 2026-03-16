using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Lite.Validation.Fluent.Runtime;
using Lite.Validation.Rules;

namespace Lite.Validation.Fluent
{
    public interface IValidationBuilder<T>
    {
        IPropertyRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> property);
        ICollectionRuleBuilder<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> property);
    }


    public class ValidationBuilder<T> : IValidationBuilder<T>
    {
        private readonly List<object> _propertyBuilders = new List<object>();
        private bool? _hasDependentRules;

        /// <summary>
        /// Gets or sets the cascade mode for how rules should execute within a single property chain.
        /// Default is Continue (validate all rules).
        /// </summary>
        public CascadeMode RuleLevelCascadeMode { get; set; } = CascadeMode.Continue;

        /// <summary>
        /// Gets or sets the cascade mode for how the validator should proceed between different properties.
        /// Default is Continue (validate all properties).
        /// </summary>
        public CascadeMode ClassLevelCascadeMode { get; set; } = CascadeMode.Continue;

        public IPropertyRuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> property)
        {
            var propertyBuilder = new PropertyValidationBuilder<T, TProperty>(property);
            propertyBuilder.Initialize(_propertyBuilders.Count, this);
            _propertyBuilders.Add(propertyBuilder);
            return propertyBuilder.CreateFluentBuilder();
        }

        public ICollectionRuleBuilder<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> property)
        {
            var builder = new CollectionPropertyValidationBuilder<T, TElement>(property);
            builder.Initialize(_propertyBuilders.Count, this);
            _propertyBuilders.Add(builder);
            return builder.CreateFluentBuilder();
        }

        /// <summary>
        /// Adds a rule set for a transformed value of a property. The transform is applied before validation.
        /// Allows changing the property type (e.g. string → DateTime).
        /// </summary>
        public IPropertyRuleBuilder<T, TOut> Transform<TIn, TOut>(
            Expression<Func<T, TIn>> property,
            Func<TIn, TOut> transform)
        {
            var originalGetter = property.Compile();
            var propertyName = GetPropertyName(property);
            Func<T, TOut> compositeGetter = target => transform(originalGetter(target));
            var builder = new PropertyValidationBuilder<T, TOut>(compositeGetter, propertyName);
            builder.Initialize(_propertyBuilders.Count, this);
            _propertyBuilders.Add(builder);
            return builder.CreateFluentBuilder();
        }

        /// <summary>
        /// Runs all rules added inside the configure action only when condition is true.
        /// </summary>
        public void When(Func<T, bool> condition, Action configure)
        {
            var countBefore = _propertyBuilders.Count;
            configure();
            for (var i = countBefore; i < _propertyBuilders.Count; i++)
                (_propertyBuilders[i] as IGroupConditionSetter<T>)?.SetGroupCondition(condition);
        }

        internal IReadOnlyList<object> GetPropertyBuilders() => _propertyBuilders;

        /// <summary>
        /// Determines if any property builder has asynchronous rules.
        /// </summary>
        public bool HasAsyncRules
        {
            get
            {
                foreach (var builder in _propertyBuilders)
                {
                    var builderType = builder.GetType();
                    var ruleBuildersProp = builderType.GetProperty("RuleBuilders");
                    if (ruleBuildersProp?.GetValue(builder) is not System.Collections.IList ruleBuilders)
                        continue;

                    foreach (var rb in ruleBuilders)
                    {
                        var rbType = rb?.GetType();
                        if (rbType?.GetProperty("AsyncRule")?.GetValue(rb) is not null)
                            return true;
                        if (rbType?.GetProperty("AsyncCondition")?.GetValue(rb) is not null)
                            return true;
                    }
                }
                return false;
            }
        }

        internal bool HasDependentRules
        {
            get
            {
                if (_hasDependentRules.HasValue) return _hasDependentRules.Value;
                foreach (var b in _propertyBuilders)
                    if ((b as IDependencyIndexProvider)?.DependencyValidatorIndex >= 0)
                    {
                        _hasDependentRules = true;
                        return true;
                    }
                _hasDependentRules = false;
                return false;
            }
        }

        /// <summary>
        /// Compiles all collected property rules into an optimized array of validators.
        /// Uses reflection once per property to resolve generic TProperty, then everything is compiled.
        /// </summary>
        internal CompiledPropertyValidatorBase<T>[] Compile()
        {
            var compiled = new CompiledPropertyValidatorBase<T>[_propertyBuilders.Count];
            for (var i = 0; i < _propertyBuilders.Count; i++)
            {
                var builder = _propertyBuilders[i];
                if (IsCollectionBuilder(builder))
                    compiled[i] = CompileCollectionValidator(builder);
                else
                    compiled[i] = CompilePropertyValidator(builder);
            }
            return compiled;
        }

        private bool IsCollectionBuilder(object builder)
        {
            var type = builder.GetType();
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CollectionPropertyValidationBuilder<,>);
        }

        private CompiledPropertyValidatorBase<T> CompilePropertyValidator(object propertyBuilderObj)
        {
            var builderType = propertyBuilderObj.GetType();
            var method = typeof(ValidationBuilder<T>)
                .GetMethod(nameof(CompilePropertyValidatorTyped),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(builderType.GetGenericArguments()[1]);

            return (CompiledPropertyValidatorBase<T>)method.Invoke(this, [propertyBuilderObj])!;
        }

        private CompiledPropertyValidatorBase<T> CompilePropertyValidatorTyped<TProperty>(object builderObj)
        {
            var builder = (PropertyValidationBuilder<T, TProperty>)builderObj;
            var getter = builder.CompiledGetter ?? builder.PropertyExpression!.Compile();
            var propertyName = builder.PropertyName;
            var ruleBuilders = builder.RuleBuilders;

            var rulesList = new List<CompiledRule<T, TProperty>>(ruleBuilders.Count);
            for (var i = 0; i < ruleBuilders.Count; i++)
            {
                var rb = ruleBuilders[i];
                if (rb.Rule is null && rb.AsyncRule is null)
                    continue;
                Func<T, TProperty, ValidationError[]>? getErrors = null;
                Func<T, TProperty, CancellationToken, ValueTask<ValidationError[]?>>? getErrorsAsync = null;
                if (rb.Rule is SetValidatorRule<T, TProperty> setValidator)
                {
                    getErrors = setValidator.Validate;
                    getErrorsAsync = setValidator.ValidateAsync;
                }
                rulesList.Add(new CompiledRule<T, TProperty>(
                    rb.Rule,
                    rb.AsyncRule,
                    rb.Details,
                    rb.Condition,
                    rb.AsyncCondition,
                    getErrors,
                    getErrorsAsync));
            }

            var depIdx = ((IDependencyIndexProvider)builder).DependencyValidatorIndex;
            return new CompiledPropertyValidator<T, TProperty>(getter, propertyName, rulesList.ToArray(), RuleLevelCascadeMode, builder.GroupCondition, depIdx);
        }

        private CompiledPropertyValidatorBase<T> CompileCollectionValidator(object collectionBuilderObj)
        {
            var builderType = collectionBuilderObj.GetType();
            var method = typeof(ValidationBuilder<T>)
                .GetMethod(nameof(CompileCollectionValidatorTyped),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(builderType.GetGenericArguments()[1]);

            return (CompiledPropertyValidatorBase<T>)method.Invoke(this, [collectionBuilderObj])!;
        }

        private CompiledPropertyValidatorBase<T> CompileCollectionValidatorTyped<TElement>(object builderObj)
        {
            var builder = (CollectionPropertyValidationBuilder<T, TElement>)builderObj;
            // Compile() method on CollectionPropertyValidationBuilder handles creating CompiledCollectionPropertyValidator
            return builder.Compile(RuleLevelCascadeMode);
        }

        private static string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;
            throw new ArgumentException("Expression must be a simple property access expression.");
        }
    }


}
