using System.Text.RegularExpressions;
using Lite.Validation.Rules.BuiltIn;
using Lite.Validation.Rules.BuiltIn.Specialized;

namespace Lite.Validation.Fluent
{
    public static class PropertyValidationBuilderExtensions
    {
        // ── Null / NotNull ──────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> NotNull<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this)
            => @this.AddRule(NotNullRule<TType, TProperty>.Shared);

        public static IPropertyRuleBuilder<TType, TProperty> Null<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this)
            => @this.AddRule(NullRule<TType, TProperty>.Shared);

        // ── Empty / NotEmpty ────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> NotEmpty<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this)
            => @this.AddRule(NotEmptyRule<TType, TProperty>.Shared);

        public static IPropertyRuleBuilder<TType, string> NotEmpty<TType>(
            this IPropertyValidationBuilder<TType, string> @this)
            => @this.AddRule(NotEmptyStringRule<TType>.Shared);

        public static IPropertyRuleBuilder<TType, TProperty> Empty<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this)
            => @this.AddRule(EmptyRule<TType, TProperty>.Shared);

        // ── Equal / NotEqual ────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> Equal<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty comparand,
            IEqualityComparer<TProperty>? comparer = null)
            => @this.AddRule(new EqualRule<TType, TProperty>(comparand, comparer));

        public static IPropertyRuleBuilder<TType, TProperty> Equal<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            Func<TType, TProperty> getComparand,
            IEqualityComparer<TProperty>? comparer = null)
            => @this.AddRule(new EqualToPropertyRule<TType, TProperty>(getComparand, comparer));

        public static IPropertyRuleBuilder<TType, TProperty> NotEqual<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty comparand,
            IEqualityComparer<TProperty>? comparer = null)
            => @this.AddRule(new NotEqualRule<TType, TProperty>(comparand, comparer));

        public static IPropertyRuleBuilder<TType, TProperty> NotEqual<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            Func<TType, TProperty> getComparand,
            IEqualityComparer<TProperty>? comparer = null)
            => @this.AddRule(new NotEqualToPropertyRule<TType, TProperty>(getComparand, comparer));

        // ── String Length ───────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, string> Length<TType>(
            this IPropertyValidationBuilder<TType, string> @this,
            int min, int max)
            => @this.AddRule(new LengthRule<TType>(min, max));

        public static IPropertyRuleBuilder<TType, string> MinimumLength<TType>(
            this IPropertyValidationBuilder<TType, string> @this,
            int min)
            => @this.AddRule(new MinimumLengthRule<TType>(min));

        public static IPropertyRuleBuilder<TType, string> MaximumLength<TType>(
            this IPropertyValidationBuilder<TType, string> @this,
            int max)
            => @this.AddRule(new MaximumLengthRule<TType>(max));

        // ── Comparison ──────────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> LessThan<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty bound) where TProperty : IComparable<TProperty>
            => @this.AddRule(new LessThanRule<TType, TProperty>(bound));

        public static IPropertyRuleBuilder<TType, TProperty> LessThanOrEqualTo<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty bound) where TProperty : IComparable<TProperty>
            => @this.AddRule(new LessThanOrEqualRule<TType, TProperty>(bound));

        public static IPropertyRuleBuilder<TType, TProperty> GreaterThan<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty bound) where TProperty : IComparable<TProperty>
            => @this.AddRule(new GreaterThanRule<TType, TProperty>(bound));

        public static IPropertyRuleBuilder<TType, TProperty> GreaterThanOrEqualTo<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty bound) where TProperty : IComparable<TProperty>
            => @this.AddRule(new GreaterThanOrEqualRule<TType, TProperty>(bound));

        // ── Between ─────────────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> ExclusiveBetween<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty from, TProperty to) where TProperty : IComparable<TProperty>
            => @this.AddRule(new ExclusiveBetweenRule<TType, TProperty>(from, to));

        public static IPropertyRuleBuilder<TType, TProperty> InclusiveBetween<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            TProperty from, TProperty to) where TProperty : IComparable<TProperty>
            => @this.AddRule(new InclusiveBetweenRule<TType, TProperty>(from, to));

        // ── String Pattern ──────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, string> Matches<TType>(
            this IPropertyValidationBuilder<TType, string> @this,
            string pattern)
            => @this.AddRule(new MatchesRule<TType>(pattern));

        public static IPropertyRuleBuilder<TType, string> Matches<TType>(
            this IPropertyValidationBuilder<TType, string> @this,
            Regex regex)
            => @this.AddRule(new MatchesRule<TType>(regex));

        public static IPropertyRuleBuilder<TType, string> EmailAddress<TType>(
            this IPropertyValidationBuilder<TType, string> @this)
            => @this.AddRule(EmailAddressRule<TType>.Shared);

        public static IPropertyRuleBuilder<TType, string> CreditCard<TType>(
            this IPropertyValidationBuilder<TType, string> @this)
            => @this.AddRule(CreditCardRule<TType>.Shared);

        // ── Enum ────────────────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> IsInEnum<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this)
            => @this.AddRule(IsInEnumRule<TType, TProperty>.Shared);

        // ── Decimal Precision ───────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, decimal> PrecisionScale<TType>(
            this IPropertyValidationBuilder<TType, decimal> @this,
            int precision, int scale, bool ignoreTrailingZeros = false)
            => @this.AddRule(new PrecisionScaleRule<TType>(precision, scale, ignoreTrailingZeros));

        // ── Predicate (Must) ────────────────────────────────────────────────

        public static IPropertyRuleBuilder<TType, TProperty> Must<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            Func<TType, TProperty, bool> validate)
            => @this.AddRule(new MustRule<TType, TProperty>(validate));

        public static IPropertyRuleBuilder<TType, TProperty> MustAsync<TType, TProperty>(
            this IPropertyValidationBuilder<TType, TProperty> @this,
            Func<TType, TProperty, ValueTask<bool>> validate)
            => @this.AddAsyncRule(new AsyncMustRule<TType, TProperty>(validate));
    }
}
