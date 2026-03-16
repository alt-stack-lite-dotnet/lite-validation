using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    /// <summary>
    /// Validates a credit card number using the Luhn algorithm.
    /// </summary>
    public class CreditCardRule<TType> : IRule<TType, string>
    {
        public static readonly CreditCardRule<TType> Shared = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (value is null) return false;

            // Strip spaces and dashes
            var sum = 0;
            var alternate = false;
            var digitCount = 0;

            for (var i = value.Length - 1; i >= 0; i--)
            {
                var ch = value[i];
                if (ch is ' ' or '-') continue;
                if (ch < '0' || ch > '9') return false;

                var n = ch - '0';
                digitCount++;

                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }

                sum += n;
                alternate = !alternate;
            }

            return digitCount >= 13 && sum % 10 == 0;
        }
    }
}
