using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    public class NotEmptyRule<TType, TProperty> : NotEmptyRuleBase<TType, TProperty>
    {
        public static readonly NotEmptyRule<TType, TProperty> Shared = new ();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsSatisfiedBy(TType target, TProperty value) =>
            NotNullRule<TType, TProperty>.Shared.IsSatisfiedBy(target, value);
    }
}