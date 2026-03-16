namespace Lite.Validation.Rules
{
    public interface IAsyncRule<in TType, in TProperty>
    {
        ValueTask<bool> IsSatisfiedByAsync(
            TType target,
            TProperty propertyValue,
            CancellationToken cancellationToken);
    }
}