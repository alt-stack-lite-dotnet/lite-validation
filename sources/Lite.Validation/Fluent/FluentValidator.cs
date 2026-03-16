using System.Threading;
using System.Threading.Tasks;

namespace Lite.Validation.Fluent
{
    /// <summary>
    /// Abstract base class for source-generated validators.
    /// Derive using <c>partial class</c> and describe rules in a <c>private</c> constructor
    /// that takes <c>ValidationBuilder&lt;T&gt;</c> as its last parameter.
    /// The source generator reads that constructor and produces a sealed
    /// <c>Validate()</c> / <c>ValidateAsync()</c> override with zero reflection.
    /// <code>
    /// public partial class OrderValidator : FluentValidator&lt;Order&gt;
    /// {
    ///     private OrderValidator(IMyService svc, ValidationBuilder&lt;Order&gt; b)
    ///     {
    ///         b.RuleFor(x => x.Amount).GreaterThan(0).WithDetails("positive");
    ///         b.RuleFor(x => x.Email).Must((_, v) => svc.IsValid(v)).WithDetails("invalid");
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class FluentValidator<T> : IValidator<T>, IAsyncValidator<T>
    {
        /// <summary>True if this validator has async rules (set by source generator).</summary>
        public abstract bool IsAsync { get; }

        /// <inheritdoc />
        public abstract ValidationResult Validate(T target);

        /// <inheritdoc />
        public abstract ValueTask<ValidationResult> ValidateAsync(
            T target, CancellationToken cancellationToken = default);
    }
}
