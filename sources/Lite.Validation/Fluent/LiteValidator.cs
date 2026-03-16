using System.Threading;
using System.Threading.Tasks;
using Lite.Validation.Fluent.Runtime;

namespace Lite.Validation.Fluent;

/// <summary>
/// Runtime validator that compiles a <see cref="ValidationBuilder{T}"/> once and runs validation.
/// Use when you build rules in code (constructor) instead of source generation.
/// </summary>
public class LiteValidator<T> : IValidator<T>, IAsyncValidator<T>
{
    private readonly CompiledPropertyValidatorBase<T>[] _compiled;

    public LiteValidator(ValidationBuilder<T> builder)
    {
        _compiled = builder.Compile();
    }

    public bool IsAsync
    {
        get
        {
            foreach (var v in _compiled)
            {
                if (v.HasAsyncRules) return true;
            }
            return false;
        }
    }

    public ValidationResult Validate(T target)
    {
        var result = new ValidationResult();
        for (var i = 0; i < _compiled.Length; i++)
            _compiled[i].Validate(target, ref result);
        return result;
    }

    public async ValueTask<ValidationResult> ValidateAsync(T target, CancellationToken cancellationToken = default)
    {
        if (!IsAsync)
            return Validate(target);

        var result = new ValidationResult();
        for (var i = 0; i < _compiled.Length; i++)
        {
            var list = await _compiled[i].ValidateAsync(target, cancellationToken).ConfigureAwait(false);
            if (list is not null && list.Count > 0)
                result.AddRange(list);
        }
        return result;
    }
}
