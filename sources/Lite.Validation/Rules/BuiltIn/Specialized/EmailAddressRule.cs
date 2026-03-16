using System.Runtime.CompilerServices;

namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    /// <summary>
    /// Simple email validation: non-empty string containing "@" not at start or end.
    /// Matches FluentValidation default mode (AspNetCoreCompatible).
    /// </summary>
    public class EmailAddressRule<TType> : IRule<TType, string>
    {
        public static readonly EmailAddressRule<TType> Shared = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (value is null) return false;

            var index = value.IndexOf('@');
            return index > 0 && index < value.Length - 1 && value.IndexOf('@', index + 1) < 0;
        }
    }
}
