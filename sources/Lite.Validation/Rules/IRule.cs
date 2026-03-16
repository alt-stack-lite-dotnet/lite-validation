namespace Lite.Validation.Rules
{
    public interface IRule<in TType, in TProperty>
    {
        bool IsSatisfiedBy(TType target, TProperty value);
    }
}