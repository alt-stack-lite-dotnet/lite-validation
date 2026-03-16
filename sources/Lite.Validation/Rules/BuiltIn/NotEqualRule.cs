using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn
{
    public class NotEqualRule<TType, TProperty> : IRule<TType, TProperty>
    {
        private readonly TProperty _comparand;
        private readonly IEqualityComparer<TProperty>? _comparer;

        public NotEqualRule(TProperty comparand, IEqualityComparer<TProperty>? comparer = null)
        {
            _comparand = comparand;
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value)
        {
            if (_comparer is not null) return !_comparer.Equals(value, _comparand);
            return !EqualityComparer<TProperty>.Default.Equals(value, _comparand);
        }
    }

    public class NotEqualToPropertyRule<TType, TProperty> : IRule<TType, TProperty>
    {
        private readonly Func<TType, TProperty> _getComparand;
        private readonly IEqualityComparer<TProperty>? _comparer;

        public NotEqualToPropertyRule(Func<TType, TProperty> getComparand, IEqualityComparer<TProperty>? comparer = null)
        {
            _getComparand = getComparand;
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, TProperty value)
        {
            var comparand = _getComparand(target);
            if (_comparer is not null) return !_comparer.Equals(value, comparand);
            return !EqualityComparer<TProperty>.Default.Equals(value, comparand);
        }
    }
}
