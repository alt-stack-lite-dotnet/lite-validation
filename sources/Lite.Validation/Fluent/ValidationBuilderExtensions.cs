using Lite.Validation.Rules;

namespace Lite.Validation.Fluent
{
    public static class ValidationBuilderExtensions
    {
        public static IPropertyRuleBuilder<T, TProperty> SetValidator<T, TProperty>(
            this IPropertyRuleBuilder<T, TProperty> builder,
            IValidator<TProperty> validator)
        {
            return builder.AddRule(new SetValidatorRule<T, TProperty>(validator));
        }
    }
}
