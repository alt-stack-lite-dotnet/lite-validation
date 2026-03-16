using System.Threading;
using System.Threading.Tasks;

namespace Lite.Validation
{
    public interface IAsyncValidator<in T> : IValidatorCore<T>
    {
        ValueTask<ValidationResult> ValidateAsync(T target, CancellationToken cancellationToken);
    }
}