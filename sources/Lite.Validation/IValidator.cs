namespace Lite.Validation
{
    public interface IValidator<in T> : IValidatorCore<T>
    {
        ValidationResult Validate(T target);
    }
}