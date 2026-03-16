using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Lite.Validation.Fluent.Runtime;
using Lite.Validation.Rules;

namespace Lite.Validation.Fluent
{
    internal class CollectionPropertyValidationBuilder<TType, TElement> :
        IPropertyValidationBuilder<TType, TElement>,
        IGroupConditionSetter<TType>,
        IDependencyIndexProvider
    {
        private readonly List<PropertyRuleBuilder<TType, TElement>> _ruleBuilders = new();
        private int _dependencyValidatorIndex = -1;

        internal Expression<Func<TType, IEnumerable<TElement>>> CollectionExpression { get; }
        internal string PropertyName { get; }
        internal IReadOnlyList<PropertyRuleBuilder<TType, TElement>> RuleBuilders => _ruleBuilders;
        internal Func<TType, bool>? GroupCondition { get; private set; }
        internal int BuilderIndex { get; private set; } = -1;
        internal ValidationBuilder<TType>? OwnerBuilder { get; private set; }

        void IGroupConditionSetter<TType>.SetGroupCondition(Func<TType, bool> condition)
            => GroupCondition = condition;

        int IDependencyIndexProvider.DependencyValidatorIndex => _dependencyValidatorIndex;
        void IDependencyIndexProvider.SetDependencyValidatorIndex(int index) => _dependencyValidatorIndex = index;

        internal void Initialize(int builderIndex, ValidationBuilder<TType> owner)
        {
            BuilderIndex = builderIndex;
            OwnerBuilder = owner;
        }

        public CollectionPropertyValidationBuilder(Expression<Func<TType, IEnumerable<TElement>>> collectionExpression)
        {
            CollectionExpression = collectionExpression;
            PropertyName = GetPropertyName(collectionExpression);
        }

        internal ICollectionRuleBuilder<TType, TElement> CreateFluentBuilder()
        {
            return new CollectionPropertyRuleBuilder<TType, TElement>(this);
        }

        public IPropertyRuleBuilder<TType, TElement> AddRule(IRule<TType, TElement> rule)
        {
            var builder = new CollectionPropertyRuleBuilder<TType, TElement>(this, rule);
            _ruleBuilders.Add(builder);
            return builder;
        }

        public IPropertyRuleBuilder<TType, TElement> AddAsyncRule(IAsyncRule<TType, TElement> rule)
        {
            var builder = new CollectionPropertyRuleBuilder<TType, TElement>(this, rule);
            _ruleBuilders.Add(builder);
            return builder;
        }

        internal CompiledPropertyValidatorBase<TType> Compile(CascadeMode ruleLevelCascadeMode)
        {
            var getter = CollectionExpression.Compile();
            var rulesList = new List<CompiledRule<TType, TElement>>();

            foreach (var rb in _ruleBuilders)
            {
                if (rb.Rule is null && rb.AsyncRule is null) continue; // Skip initial empty builder

                Func<TType, TElement, ValidationError[]>? getErrors = null;
                Func<TType, TElement, CancellationToken, ValueTask<ValidationError[]?>>? getErrorsAsync = null;
                if (rb.Rule is SetValidatorRule<TType, TElement> setValidator)
                {
                    getErrors = setValidator.Validate;
                    getErrorsAsync = setValidator.ValidateAsync;
                }
                rulesList.Add(new CompiledRule<TType, TElement>(
                    rb.Rule,
                    rb.AsyncRule,
                    rb.Details,
                    rb.Condition,
                    rb.AsyncCondition,
                    getErrors,
                    getErrorsAsync));
            }

            return new CompiledCollectionPropertyValidator<TType, TElement>(getter, PropertyName, rulesList.ToArray(), ruleLevelCascadeMode, GroupCondition, _dependencyValidatorIndex);
        }

        private static string GetPropertyName(Expression<Func<TType, IEnumerable<TElement>>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;
            throw new ArgumentException("Expression must be a simple property access expression.");
        }
    }

    internal class CollectionPropertyRuleBuilder<TType, TElement> :
        PropertyRuleBuilder<TType, TElement>,
        ICollectionRuleBuilder<TType, TElement>
    {
        public CollectionPropertyRuleBuilder(IPropertyValidationBuilder<TType, TElement> parentBuilder)
            : base(parentBuilder) { }

        public CollectionPropertyRuleBuilder(IPropertyValidationBuilder<TType, TElement> parentBuilder, IRule<TType, TElement> rule)
            : base(parentBuilder, rule) { }

        public CollectionPropertyRuleBuilder(IPropertyValidationBuilder<TType, TElement> parentBuilder, IAsyncRule<TType, TElement> asyncRule)
            : base(parentBuilder, asyncRule) { }
    }
}
