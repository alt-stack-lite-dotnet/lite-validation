namespace Lite.Validation.Rules.BuiltIn.Specialized
{
    public class NotEmptyStringRule<TType> : NotEmptyRuleBase<TType, string>
    {
        public static readonly NotEmptyStringRule<TType> Shared = new ();
        public override bool IsSatisfiedBy(TType target, string value) => !string.IsNullOrWhiteSpace(value);
    }
}