using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using FluentValidation;
using Lite.Validation.Fluent;
using LiteValidationResult = Lite.Validation.ValidationResult;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Lite.Benchmarks.Validation;

/// <summary>Source-generated validator (same rules as LiteSimpleValidator).</summary>
public partial class LiteSgValidator : FluentValidator<TestModel>
{
    static void Configure(ValidationBuilder<TestModel> b)
    {
        b.RuleFor(x => x.Name).NotEmpty().Length(2, 100);
        b.RuleFor(x => x.Email).NotEmpty().EmailAddress();
        b.RuleFor(x => x.Age).InclusiveBetween(0, 150);
    }
}

[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class SimpleValidationBenchmark
{
    private readonly TestModel _validModel;
    private readonly TestModel _invalidModel;
    private readonly DataAnnotationsModel _validDA;
    private readonly DataAnnotationsModel _invalidDA;
    private readonly FluentValidation.IValidator<TestModel> _fluentValidator;
    private readonly Lite.Validation.IValidator<TestModel> _liteValidator;
    private readonly Lite.Validation.IValidator<TestModel> _liteSgValidator;
    private readonly List<DataAnnotationsValidationResult> _daResults = new();

    public SimpleValidationBenchmark()
    {
        _validModel = new TestModel { Name = "John Doe", Email = "john@example.com", Age = 25 };
        _invalidModel = new TestModel { Name = "", Email = "invalid", Age = -5 };
        _validDA = new DataAnnotationsModel { Name = "John Doe", Email = "john@example.com", Age = 25 };
        _invalidDA = new DataAnnotationsModel { Name = "", Email = "invalid", Age = -5 };

        _fluentValidator = new FluentSimpleValidator();
        _liteValidator = new LiteSimpleValidator(new ValidationBuilder<TestModel>());
        _liteSgValidator = new LiteSgValidator();
    }

    // --- FluentValidation ---

    [Benchmark(Baseline = true)]
    public FluentValidation.Results.ValidationResult FluentValidation_Valid()
        => _fluentValidator.Validate(_validModel);

    [Benchmark]
    public FluentValidation.Results.ValidationResult FluentValidation_Invalid()
        => _fluentValidator.Validate(_invalidModel);

    // --- Lite.Validation (Runtime) ---

    [Benchmark]
    public LiteValidationResult LiteRuntime_Valid()
        => _liteValidator.Validate(_validModel);

    [Benchmark]
    public LiteValidationResult LiteRuntime_Invalid()
        => _liteValidator.Validate(_invalidModel);

    // --- Lite.Validation (Source-generated) ---

    [Benchmark]
    public LiteValidationResult LiteSg_Valid()
        => _liteSgValidator.Validate(_validModel);

    [Benchmark]
    public LiteValidationResult LiteSg_Invalid()
        => _liteSgValidator.Validate(_invalidModel);

    // --- DataAnnotations ---

    [Benchmark]
    public bool DataAnnotations_Valid()
    {
        _daResults.Clear();
        return Validator.TryValidateObject(_validDA, new ValidationContext(_validDA), _daResults, true);
    }

    [Benchmark]
    public bool DataAnnotations_Invalid()
    {
        _daResults.Clear();
        return Validator.TryValidateObject(_invalidDA, new ValidationContext(_invalidDA), _daResults, true);
    }
}

// --- Models ---

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class DataAnnotationsModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(0, 150)]
    public int Age { get; set; }
}

// --- Validators ---

public class LiteSimpleValidator : LiteValidator<TestModel>
{
    public LiteSimpleValidator(ValidationBuilder<TestModel> builder) : base(builder)
    {
        builder.RuleFor(x => x.Name).NotEmpty().Length(2, 100);
        builder.RuleFor(x => x.Email).NotEmpty().EmailAddress();
        builder.RuleFor(x => x.Age).InclusiveBetween(0, 150);
    }
}

public class FluentSimpleValidator : AbstractValidator<TestModel>
{
    public FluentSimpleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(0, 150);
    }
}
