namespace Lite.Validation.Rules.BuiltIn
{
    public abstract class NotEmptyRuleBase<TType, TProperty> : IRule<TType, TProperty>
    {
        public abstract bool IsSatisfiedBy(TType target, TProperty value);
    }
}