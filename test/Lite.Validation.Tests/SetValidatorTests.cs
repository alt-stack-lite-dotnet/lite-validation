using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

public class SetValidatorTests
{
    public record ChildModel(string Name, int Value);
    public record ParentModel(string Description, ChildModel Child, List<ChildModel> Children);

    public class ChildValidator : LiteValidator<ChildModel>
    {
        public ChildValidator(ValidationBuilder<ChildModel> b) : base(b)
        {
            b.RuleFor(x => x.Name).NotNull().WithDetails("Name is required");
            b.RuleFor(x => x.Value).GreaterThan(0).WithDetails("Value must be positive");
        }
    }

    public class AsyncChildValidator : LiteValidator<ChildModel>
    {
        public AsyncChildValidator(ValidationBuilder<ChildModel> b) : base(b)
        {
            b.RuleFor(x => x.Name).NotNull().WithDetails("Name is required");
            b.RuleFor(x => x.Value)
                .WhenAsync(x => ValueTask.FromResult(true))
                .GreaterThan(0).WithDetails("Value must be positive");
        }
    }

    public class ParentValidator : LiteValidator<ParentModel>
    {
        public ParentValidator(ValidationBuilder<ParentModel> b) : base(b)
        {
            b.RuleFor(x => x.Description).NotNull();
            b.RuleFor(x => x.Child).SetValidator(new ChildValidator(new ValidationBuilder<ChildModel>()));
            b.RuleForEach(x => x.Children).SetValidator(new ChildValidator(new ValidationBuilder<ChildModel>()));
        }
    }

    public class AsyncParentValidator : LiteValidator<ParentModel>
    {
        public AsyncParentValidator(ValidationBuilder<ParentModel> b) : base(b)
        {
            b.RuleFor(x => x.Child).SetValidator(new AsyncChildValidator(new ValidationBuilder<ChildModel>()));
            b.RuleForEach(x => x.Children).SetValidator(new AsyncChildValidator(new ValidationBuilder<ChildModel>()));
        }
    }

    private static ValidationBuilder<T> B<T>() => new();

    [Fact]
    public void SetValidator_ValidatesNestedProperty()
    {
        var validator = new ParentValidator(B<ParentModel>());
        var model = new ParentModel("Test", new ChildModel(null!, -1), new List<ChildModel>());

        var result = validator.Validate(model);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Child.Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Child.Value");
    }

    [Fact]
    public void SetValidator_ValidatesCollection()
    {
        var validator = new ParentValidator(B<ParentModel>());
        var model = new ParentModel("Test", new ChildModel("Valid", 10), new List<ChildModel>
        {
            new ChildModel("Valid", 10),
            new ChildModel(null!, -5)
        });

        var result = validator.Validate(model);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Children[1].Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Children[1].Value");
    }

    [Fact]
    public async Task SetValidator_Async_ValidatesNestedProperty()
    {
        var validator = new AsyncParentValidator(B<ParentModel>());
        var model = new ParentModel("Test", new ChildModel(null!, -1), new List<ChildModel>());

        var result = await validator.ValidateAsync(model);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Child.Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Child.Value");
    }

    [Fact]
    public async Task SetValidator_Async_ValidatesCollection()
    {
        var validator = new AsyncParentValidator(B<ParentModel>());
        var model = new ParentModel("Test", new ChildModel("Valid", 10), new List<ChildModel>
        {
            new ChildModel(null!, -5)
        });

        var result = await validator.ValidateAsync(model);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Children[0].Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Children[0].Value");
    }

    [Fact]
    public void SetValidator_NullProperty_Skipped()
    {
        var validator = new ParentValidator(B<ParentModel>());
        var model = new ParentModel("Test", null!, new List<ChildModel>());

        var result = validator.Validate(model);

        Assert.True(result.IsSuccess);
    }
}
