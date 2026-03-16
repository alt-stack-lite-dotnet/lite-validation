using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    /// <summary>
    /// Ensures a string matches a regular expression.
    /// </summary>
    public class MatchesRule<TType> : IRule<TType, string>
    {
        private readonly Regex _regex;

        public MatchesRule(string pattern) => _regex = new Regex(pattern, RegexOptions.Compiled);
        public MatchesRule(Regex regex) => _regex = regex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSatisfiedBy(TType target, string value)
        {
            if (value is null) return false;
            return _regex.IsMatch(value);
        }
    }
}
