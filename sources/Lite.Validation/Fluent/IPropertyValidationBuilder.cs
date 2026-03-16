using System.Linq.Expressions;
using Lite.Validation.Rules;

namespace Lite.Validation.Fluent
{
    public interface IPropertyValidationBuilder<TType, TProperty>
    {
        IPropertyRuleBuilder<TType, TProperty> AddRule(IRule<TType, TProperty> rule);
        IPropertyRuleBuilder<TType, TProperty> AddAsyncRule(IAsyncRule<TType, TProperty> rule);
    }

    internal interface IGroupConditionSetter<TType>
    {
        void SetGroupCondition(Func<TType, bool> condition);
    }

    internal interface IDependencyIndexProvider
    {
        int DependencyValidatorIndex { get; }
        void SetDependencyValidatorIndex(int index);
    }

    internal class PropertyValidationBuilder<TType, TProperty> :
        IPropertyValidationBuilder<TType, TProperty>,
        IGroupConditionSetter<TType>,
        IDependencyIndexProvider
    {
        private readonly List<PropertyRuleBuilder<TType, TProperty>> _ruleBuilders = new();
        private int _dependencyValidatorIndex = -1;

        internal Expression<Func<TType, TProperty>>? PropertyExpression { get; }
        internal Func<TType, TProperty>? CompiledGetter { get; }
        internal string PropertyName { get; }
        internal IReadOnlyList<PropertyRuleBuilder<TType, TProperty>> RuleBuilders => _ruleBuilders;
        internal Func<TType, bool>? GroupCondition { get; private set; }
        internal int BuilderIndex { get; private set; } = -1;
        internal ValidationBuilder<TType>? OwnerBuilder { get; private set; }

        // Standard ctor: from expression (RuleFor)
        public PropertyValidationBuilder(Expression<Func<TType, TProperty>> propertyExpression)
        {
            PropertyExpression = propertyExpression;
            PropertyName = GetPropertyName(propertyExpression);
        }

        // Transform ctor: pre-compiled getter (Transform<TIn, TOut>)
        internal PropertyValidationBuilder(Func<TType, TProperty> compiledGetter, string propertyName)
        {
            CompiledGetter = compiledGetter;
            PropertyName = propertyName;
        }

        internal void Initialize(int builderIndex, ValidationBuilder<TType> owner)
        {
            BuilderIndex = builderIndex;
            OwnerBuilder = owner;
        }

        void IGroupConditionSetter<TType>.SetGroupCondition(Func<TType, bool> condition)
            => GroupCondition = condition;

        int IDependencyIndexProvider.DependencyValidatorIndex => _dependencyValidatorIndex;
        void IDependencyIndexProvider.SetDependencyValidatorIndex(int index) => _dependencyValidatorIndex = index;

        internal PropertyRuleBuilder<TType, TProperty> CreateFluentBuilder()
        {
            // Entry-point builder — not added to _ruleBuilders because it has no rule.
            // Actual rule builders are created via AddRule/AddAsyncRule and stored in _ruleBuilders.
            return new PropertyRuleBuilder<TType, TProperty>(this);
        }

        public IPropertyRuleBuilder<TType, TProperty> AddRule(IRule<TType, TProperty> rule)
        {
            var builder = new PropertyRuleBuilder<TType, TProperty>(this, rule);
            _ruleBuilders.Add(builder);
            return builder;
        }

        public IPropertyRuleBuilder<TType, TProperty> AddAsyncRule(IAsyncRule<TType, TProperty> rule)
        {
            var builder = new PropertyRuleBuilder<TType, TProperty>(this, rule);
            _ruleBuilders.Add(builder);
            return builder;
        }

        private static string GetPropertyName(Expression<Func<TType, TProperty>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;
            throw new ArgumentException("Expression must be a simple property access expression.");
        }
    }
}
