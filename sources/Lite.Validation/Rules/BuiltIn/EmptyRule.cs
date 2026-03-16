using System.Collections;
using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    /// <summary>
    /// Opposite of NotEmpty. Checks if the value is null, default, empty string, or empty collection.
    /// </summary>
    public class EmptyRule<TType, TProperty> : IRule<TType, TProperty>
    {
        public static readonly EmptyRule<TType, TProperty> Shared = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value)
        {
            if (value is null) return true;
            if (value is string s) return s.Length == 0;
            if (value is ICollection { Count: 0 }) return true;
            if (value is IEnumerable enumerable && !enumerable.GetEnumerator().MoveNext()) return true;
            return EqualityComparer<TProperty>.Default.Equals(value, default!);
        }
    }
}
