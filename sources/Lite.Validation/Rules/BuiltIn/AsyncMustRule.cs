namespace Lite.Validation.Rules.BuiltIn
{
    public class AsyncMustRule<TType, TProperty> : IAsyncRule<TType, TProperty>
    {
        private readonly Func<TType, TProperty, ValueTask<bool>> _validate;

        public AsyncMustRule(Func<TType, TProperty, ValueTask<bool>> validate) => _validate = validate;

        public ValueTask<bool> IsSatisfiedByAsync(
            TType target,
            TProperty value,
            CancellationToken cancellationToken) => _validate.Invoke(target, value);
    }
}