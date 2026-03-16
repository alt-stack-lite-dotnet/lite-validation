using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

public class LiteValidatorTests
{
    private static CreateUserRequestValidator CreateUserValidator() =>
        new(new ValidationBuilder<CreateUserRequest>());

    private static CreateOrderRequestValidator CreateOrderValidator() =>
        new(new ValidationBuilder<CreateOrderRequest>());

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest("John", "john@test.com", 25));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_NullName_ReturnsError()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest(null, "john@test.com", 25));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Details == "Name is required");
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest("   ", "john@test.com", 25));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Details == "Name must not be empty");
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest("John", "invalid-email", 25));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.Details == "Email must contain @");
    }

    [Fact]
    public void Validate_UnderageUser_ReturnsError()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest("John", "john@test.com", 15));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Age" && e.Details == "Must be at least 18 years old");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var validator = CreateUserValidator();
        var result = validator.Validate(new CreateUserRequest(null, null, 10));

        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count >= 4);
    }

    [Fact]
    public void Validate_OrderValidRequest_ReturnsSuccess()
    {
        var validator = CreateOrderValidator();
        var result = validator.Validate(new CreateOrderRequest("Widget", 5, 9.99m));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_OrderZeroQuantity_ReturnsError()
    {
        var validator = CreateOrderValidator();
        var result = validator.Validate(new CreateOrderRequest("Widget", 0, 9.99m));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity" && e.Details == "Quantity must be positive");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_ReturnsSuccess()
    {
        var validator = CreateUserValidator();
        var result = await validator.ValidateAsync(new CreateUserRequest("John", "john@test.com", 25));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateAsync_InvalidRequest_ReturnsErrors()
    {
        var validator = CreateUserValidator();
        var result = await validator.ValidateAsync(new CreateUserRequest(null, "bad", 10));

        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count >= 3);
    }

    [Fact]
    public void Validate_ImplementsIValidator()
    {
        var validator = CreateUserValidator();

        Assert.IsAssignableFrom<IValidator<CreateUserRequest>>(validator);
        Assert.IsAssignableFrom<IAsyncValidator<CreateUserRequest>>(validator);
    }
}
