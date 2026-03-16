namespace Lite.Validation.Rules.BuiltIn
{
    public class NotNullRule<TType, TProperty> : IRule<TType, TProperty>
    {
        public static readonly NotNullRule<TType, TProperty> Shared = new();
    
        public bool IsSatisfiedBy(TType target, TProperty value) => value is not null;
    }
}