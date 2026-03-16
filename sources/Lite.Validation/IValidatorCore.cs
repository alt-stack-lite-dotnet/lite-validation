namespace Lite.Validation
{
    public interface IValidatorCore
    {
        bool IsAsync { get; }
    }

    public interface IValidatorCore<in T> : IValidatorCore { }
}
