using Lite.Validation.Rules;

namespace Lite.Validation.Fluent
{
    public interface IPropertyRuleBuilder<TType, TProperty> : IPropertyValidationBuilder<TType, TProperty>
    {
        IPropertyRuleBuilder<TType, TProperty> WithDetails(string details);
        IPropertyRuleBuilder<TType, TProperty> When(Func<TType, bool> rule);
        IPropertyRuleBuilder<TType, TProperty> WhenAsync(Func<TType, ValueTask<bool>> rule);
        IPropertyRuleBuilder<TType, TProperty> Unless(Func<TType, bool> condition);
        IPropertyRuleBuilder<TType, TProperty> UnlessAsync(Func<TType, ValueTask<bool>> condition);
        IPropertyRuleBuilder<TType, TProperty> DependentRules(Action configure);
    }


    internal class PropertyRuleBuilder<TType, TProperty> : IPropertyRuleBuilder<TType, TProperty>
    {
        private readonly IPropertyValidationBuilder<TType, TProperty> _parentBuilder;

        private string? _details;
        private Func<TType, bool>? _when;
        private Func<TType, ValueTask<bool>>? _whenAsync;

        internal IRule<TType, TProperty>? Rule { get; }
        internal IAsyncRule<TType, TProperty>? AsyncRule { get; }
        internal string? Details => _details;
        internal Func<TType, bool>? Condition => _when;
        internal Func<TType, ValueTask<bool>>? AsyncCondition => _whenAsync;
        internal bool IsAsync => AsyncRule is not null;

        // Entry-point builder (no rule yet — used by CreateFluentBuilder for initial chaining)
        public PropertyRuleBuilder(IPropertyValidationBuilder<TType, TProperty> parentBuilder)
        {
            _parentBuilder = parentBuilder;
        }

        public PropertyRuleBuilder(IPropertyValidationBuilder<TType, TProperty> parentBuilder, IRule<TType, TProperty> rule)
        {
            _parentBuilder = parentBuilder;
            Rule = rule;
        }

        public PropertyRuleBuilder(IPropertyValidationBuilder<TType, TProperty> parentBuilder, IAsyncRule<TType, TProperty> asyncRule)
        {
            _parentBuilder = parentBuilder;
            AsyncRule = asyncRule;
        }

        public IPropertyRuleBuilder<TType, TProperty> WithDetails(string details)
        {
            _details = details;
            return this;
        }

        public IPropertyRuleBuilder<TType, TProperty> When(Func<TType, bool> condition)
        {
            _when = condition;
            return this;
        }

        public IPropertyRuleBuilder<TType, TProperty> WhenAsync(Func<TType, ValueTask<bool>> condition)
        {
            _whenAsync = condition;
            return this;
        }

        public IPropertyRuleBuilder<TType, TProperty> Unless(Func<TType, bool> condition)
            => When(x => !condition(x));

        public IPropertyRuleBuilder<TType, TProperty> UnlessAsync(Func<TType, ValueTask<bool>> condition)
            => WhenAsync(async x => !await condition(x).ConfigureAwait(false));

        public IPropertyRuleBuilder<TType, TProperty> DependentRules(Action configure)
        {
            var pvb = (PropertyValidationBuilder<TType, TProperty>)_parentBuilder;
            var vb = pvb.OwnerBuilder!;
            var parentIndex = pvb.BuilderIndex;
            var countBefore = vb.GetPropertyBuilders().Count;
            configure();
            var builders = vb.GetPropertyBuilders();
            for (var i = countBefore; i < builders.Count; i++)
                (builders[i] as IDependencyIndexProvider)?.SetDependencyValidatorIndex(parentIndex);
            return this;
        }

        public IPropertyRuleBuilder<TType, TProperty> AddRule(IRule<TType, TProperty> rule) =>
            _parentBuilder.AddRule(rule);

        public IPropertyRuleBuilder<TType, TProperty> AddAsyncRule(IAsyncRule<TType, TProperty> rule) =>
            _parentBuilder.AddAsyncRule(rule);
    }
}
