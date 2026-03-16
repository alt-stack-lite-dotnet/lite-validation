using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

public partial class CreateUserRequestValidator : LiteValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(ValidationBuilder<CreateUserRequest> builder) : base(builder)
    {
        builder.RuleFor(x => x.Name)
            .NotNull().WithDetails("Name is required")
            .NotEmpty().WithDetails("Name must not be empty");

        builder.RuleFor(x => x.Email)
            .NotNull().WithDetails("Email is required")
            .Must((t, v) => v is not null && v.Contains("@")).WithDetails("Email must contain @");

        builder.RuleFor(x => x.Age)
            .Must((t, v) => v >= 18).WithDetails("Must be at least 18 years old");
    }
}

public partial class CreateOrderRequestValidator : LiteValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(ValidationBuilder<CreateOrderRequest> builder) : base(builder)
    {
        builder.RuleFor(x => x.ProductName)
            .NotNull().WithDetails("Product name is required")
            .NotEmpty().WithDetails("Product name must not be empty");

        builder.RuleFor(x => x.Quantity)
            .Must((t, v) => v > 0).WithDetails("Quantity must be positive");

        builder.RuleFor(x => x.Price)
            .Must((t, v) => v > 0m).WithDetails("Price must be positive");
    }
}
