using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    /// <summary>
    /// Checks value is strictly between (from, to) — exclusive on both ends.
    /// </summary>
    public class ExclusiveBetweenRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _from;
        private readonly TProperty _to;

        public ExclusiveBetweenRule(TProperty from, TProperty to)
        {
            _from = from;
            _to = to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) =>
            value.CompareTo(_from) > 0 && value.CompareTo(_to) < 0;
    }

    /// <summary>
    /// Checks value is between [from, to] — inclusive on both ends.
    /// </summary>
    public class InclusiveBetweenRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _from;
        private readonly TProperty _to;

        public InclusiveBetweenRule(TProperty from, TProperty to)
        {
            _from = from;
            _to = to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) =>
            value.CompareTo(_from) >= 0 && value.CompareTo(_to) <= 0;
    }
}
