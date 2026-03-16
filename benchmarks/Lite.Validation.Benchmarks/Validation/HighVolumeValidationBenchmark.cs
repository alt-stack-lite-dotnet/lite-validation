using BenchmarkDotNet.Attributes;
using FluentValidation;
using Lite.Validation.Fluent;
using LiteValidationResult = Lite.Validation.ValidationResult;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Lite.Validation.Benchmarks.Validation;

/// <summary>
/// Throughput benchmark: 10,000 validations per iteration.
/// Reports total time and allocation for 10k requests so we can compare GC impact.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class HighVolumeValidationBenchmark
{
    private const int RequestCount = 10_000;

    private readonly TestModel _validModel;
    private readonly TestModel _invalidModel;
    private readonly FluentValidation.IValidator<TestModel> _fluentValidator;
    private readonly Lite.Validation.IValidator<TestModel> _liteSgValidator;

    public HighVolumeValidationBenchmark()
    {
        _validModel = new TestModel { Name = "John Doe", Email = "john@example.com", Age = 25 };
        _invalidModel = new TestModel { Name = "", Email = "invalid", Age = -5 };
        _fluentValidator = new FluentSimpleValidator();
        _liteSgValidator = new LiteSgValidator();
    }

    /// <summary>10,000 valid (success) validations — FluentValidation.</summary>
    [Benchmark(Baseline = true)]
    public int FluentValidation_10k_Valid()
    {
        int failures = 0;
        for (int i = 0; i < RequestCount; i++)
        {
            FluentValidationResult r = _fluentValidator.Validate(_validModel);
            if (!r.IsValid) failures++;
        }
        return failures;
    }

    /// <summary>10,000 invalid (with errors) validations — FluentValidation.</summary>
    [Benchmark]
    public int FluentValidation_10k_Invalid()
    {
        int failures = 0;
        for (int i = 0; i < RequestCount; i++)
        {
            FluentValidationResult r = _fluentValidator.Validate(_invalidModel);
            if (r.IsValid) failures++;
        }
        return failures;
    }

    /// <summary>10,000 valid validations — Lite.Validation (SourceGen).</summary>
    [Benchmark]
    public int LiteSg_10k_Valid()
    {
        int failures = 0;
        for (int i = 0; i < RequestCount; i++)
        {
            var r = _liteSgValidator.Validate(_validModel);
            if (!r.IsSuccess) failures++;
        }
        return failures;
    }

    /// <summary>10,000 invalid validations — Lite.Validation (SourceGen).</summary>
    [Benchmark]
    public int LiteSg_10k_Invalid()
    {
        int failures = 0;
        for (int i = 0; i < RequestCount; i++)
        {
            var r = _liteSgValidator.Validate(_invalidModel);
            if (r.IsSuccess) failures++;
        }
        return failures;
    }
}
