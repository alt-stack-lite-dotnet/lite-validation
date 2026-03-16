using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    public class LessThanRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _bound;

        public LessThanRule(TProperty bound) => _bound = bound;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) => value.CompareTo(_bound) < 0;
    }

    public class LessThanOrEqualRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _bound;

        public LessThanOrEqualRule(TProperty bound) => _bound = bound;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) => value.CompareTo(_bound) <= 0;
    }

    public class GreaterThanRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _bound;

        public GreaterThanRule(TProperty bound) => _bound = bound;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) => value.CompareTo(_bound) > 0;
    }

    public class GreaterThanOrEqualRule<TType, TProperty> : IRule<TType, TProperty>
        where TProperty : IComparable<TProperty>
    {
        private readonly TProperty _bound;

        public GreaterThanOrEqualRule(TProperty bound) => _bound = bound;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value) => value.CompareTo(_bound) >= 0;
    }
}
