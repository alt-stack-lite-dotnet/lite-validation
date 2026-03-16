using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    /// <summary>
    /// Checks whether a value is a valid defined member of an enum type.
    /// Works with the property's own type if it is an enum.
    /// </summary>
    public class IsInEnumRule<TType, TProperty> : IRule<TType, TProperty>
    {
        public static readonly IsInEnumRule<TType, TProperty> Shared = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value)
        {
            if (value is null) return false;
            return Enum.IsDefined(typeof(TProperty), value);
        }
    }
}
