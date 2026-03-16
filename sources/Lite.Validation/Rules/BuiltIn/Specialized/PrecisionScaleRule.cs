using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    /// <summary>
    /// Checks whether a decimal value has the specified precision and scale.
    /// Precision = total number of significant digits.
    /// Scale = number of digits after decimal point.
    /// </summary>
    public class PrecisionScaleRule<TType> : IRule<TType, decimal>
    {
        private readonly int _precision;
        private readonly int _scale;
        private readonly bool _ignoreTrailingZeros;

        public PrecisionScaleRule(int precision, int scale, bool ignoreTrailingZeros = false)
        {
            _precision = precision;
            _scale = scale;
            _ignoreTrailingZeros = ignoreTrailingZeros;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, decimal value)
        {
            var absValue = Math.Abs(value);
            var text = absValue.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (_ignoreTrailingZeros)
                text = text.Contains('.') ? text.TrimEnd('0').TrimEnd('.') : text;

            var dotIndex = text.IndexOf('.');
            int integerDigits;
            int decimalDigits;

            if (dotIndex < 0)
            {
                integerDigits = text.Length;
                decimalDigits = 0;
            }
            else
            {
                integerDigits = dotIndex;
                decimalDigits = text.Length - dotIndex - 1;
            }

            // Remove leading zeros from integer part for precision calculation
            var leadingZeros = 0;
            for (var i = 0; i < integerDigits; i++)
            {
                if (text[i] == '0') leadingZeros++;
                else break;
            }
            var significantIntDigits = Math.Max(integerDigits - leadingZeros, 0);
            var totalSignificantDigits = significantIntDigits + decimalDigits;

            if (totalSignificantDigits == 0) totalSignificantDigits = 1; // "0" has precision 1

            return decimalDigits <= _scale && totalSignificantDigits <= _precision;
        }
    }
}
