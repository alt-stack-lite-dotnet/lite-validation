using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    public class NullRule<TType, TProperty> : IRule<TType, TProperty>
    {
        public static readonly NullRule<TType, TProperty> Shared = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) => value is null;
    }
}
