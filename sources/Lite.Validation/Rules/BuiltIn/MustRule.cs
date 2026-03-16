namespace Lite.Validation.Rules.BuiltIn
{
    public class MustRule<TType, TProperty> : IRule<TType, TProperty>
    {
        private readonly Func<TType, TProperty, bool> _validate;

        public MustRule(Func<TType, TProperty, bool> validate) => _validate = validate;

        public bool IsSatisfiedBy(TType target, TProperty value) => _validate.Invoke(target, value);
    }
}