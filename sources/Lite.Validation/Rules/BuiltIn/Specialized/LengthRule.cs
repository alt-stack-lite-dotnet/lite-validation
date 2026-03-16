using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    /// <summary>
    /// Ensures a string length is within [min, max] range.
    /// </summary>
    public class LengthRule<TType> : IRule<TType, string>
    {
        private readonly int _min;
        private readonly int _max;

        public LengthRule(int min, int max)
        {
            _min = min;
            _max = max;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.Length >= _min && value.Length <= _max;
        }
    }

    /// <summary>
    /// Ensures a string length is at least min characters.
    /// </summary>
    public class MinimumLengthRule<TType> : IRule<TType, string>
    {
        private readonly int _min;

        public MinimumLengthRule(int min) => _min = min;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.Length >= _min;
        }
    }

    /// <summary>
    /// Ensures a string length does not exceed max characters.
    /// </summary>
    public class MaximumLengthRule<TType> : IRule<TType, string>
    {
        private readonly int _max;

        public MaximumLengthRule(int max) => _max = max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true; // null has no length to exceed
            return value.Length <= _max;
        }
    }
}
