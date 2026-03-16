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
    private readonly ValidationBuilder<T> _builder;
    private readonly Lazy<CompiledPropertyValidatorBase<T>[]> _compiled;

    public LiteValidator(ValidationBuilder<T> builder)
    {
        _builder = builder;
        _compiled = new Lazy<CompiledPropertyValidatorBase<T>[]>(() => _builder.Compile());
    }

    private CompiledPropertyValidatorBase<T>[] Compiled => _compiled.Value;

    public bool IsAsync
    {
        get
        {
            foreach (var v in Compiled)
            {
                if (v.HasAsyncRules) return true;
            }
            return false;
        }
    }

    public ValidationResult Validate(T target)
    {
        var result = new ValidationResult();
        var compiled = Compiled;
        var addedCount = new int[compiled.Length];
        for (var i = 0; i < compiled.Length; i++)
        {
            if (compiled[i].DependencyValidatorIndex >= 0)
            {
                var parentIdx = compiled[i].DependencyValidatorIndex;
                if (parentIdx < addedCount.Length && addedCount[parentIdx] > 0)
                    continue;
            }
            var before = result.ErrorCount;
            compiled[i].Validate(target, ref result);
            addedCount[i] = result.ErrorCount - before;
        }
        return result;
    }

    public async ValueTask<ValidationResult> ValidateAsync(T target, CancellationToken cancellationToken = default)
    {
        if (!IsAsync)
            return Validate(target);

        var result = new ValidationResult();
        var compiled = Compiled;
        var addedCount = new int[compiled.Length];
        for (var i = 0; i < compiled.Length; i++)
        {
            if (compiled[i].DependencyValidatorIndex >= 0)
            {
                var parentIdx = compiled[i].DependencyValidatorIndex;
                if (parentIdx < addedCount.Length && addedCount[parentIdx] > 0)
                    continue;
            }
            var before = result.ErrorCount;
            var list = await compiled[i].ValidateAsync(target, cancellationToken).ConfigureAwait(false);
            if (list is not null && list.Count > 0)
            {
                result.AddRange(list);
                addedCount[i] = list.Count;
            }
        }
        return result;
    }
}
