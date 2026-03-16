using Lite.Validation.Fluent;

namespace Lite.Validation.Tests;

// ── Test model ──────────────────────────────────────────────────────────

public record FullTestModel(
    string? Name,
    string? Email,
    string? CardNumber,
    int Age,
    decimal Price,
    string? Password,
    string? ConfirmPassword,
    TestEnum Status);

public enum TestEnum { Active = 1, Inactive = 2 }

// ── Validators ─────────────────────────────────────────────────────────

public partial class NullRuleValidator : LiteValidator<FullTestModel>
{
    public NullRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Name).Null().WithDetails("Must be null");
    }
}

public partial class EmptyRuleValidator : LiteValidator<FullTestModel>
{
    public EmptyRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Name).Empty<FullTestModel, string?>().WithDetails("Must be empty");
    }
}

public partial class EqualRuleValidator : LiteValidator<FullTestModel>
{
    public EqualRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Age).Equal(18).WithDetails("Must be 18");
        b.RuleFor(x => x.ConfirmPassword)
            .Equal<FullTestModel, string?>(t => t.Password).WithDetails("Passwords must match");
    }
}

public partial class NotEqualRuleValidator : LiteValidator<FullTestModel>
{
    public NotEqualRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Age).NotEqual(0).WithDetails("Must not be zero");
    }
}

public partial class LengthRuleValidator : LiteValidator<FullTestModel>
{
    public LengthRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Name!)
            .Length<FullTestModel>(2, 50).WithDetails("Name must be 2-50 chars")
            .MinimumLength<FullTestModel>(2).WithDetails("Name too short")
            .MaximumLength<FullTestModel>(50).WithDetails("Name too long");
    }
}

public partial class ComparisonRuleValidator : LiteValidator<FullTestModel>
{
    public ComparisonRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Age)
            .GreaterThan(0).WithDetails("Age must be > 0")
            .GreaterThanOrEqualTo(1).WithDetails("Age must be >= 1")
            .LessThan(200).WithDetails("Age must be < 200")
            .LessThanOrEqualTo(199).WithDetails("Age must be <= 199");
    }
}

public partial class BetweenRuleValidator : LiteValidator<FullTestModel>
{
    public BetweenRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Age)
            .InclusiveBetween(1, 150).WithDetails("Age must be 1-150")
            .ExclusiveBetween(0, 151).WithDetails("Age must be between 0 and 151 exclusive");
    }
}

public partial class EmailRuleValidator : LiteValidator<FullTestModel>
{
    public EmailRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Email!).EmailAddress<FullTestModel>().WithDetails("Invalid email");
    }
}

public partial class MatchesRuleValidator : LiteValidator<FullTestModel>
{
    public MatchesRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Name!).Matches<FullTestModel>(@"^[A-Z]").WithDetails("Must start with uppercase");
    }
}

public partial class CreditCardRuleValidator : LiteValidator<FullTestModel>
{
    public CreditCardRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.CardNumber!).CreditCard<FullTestModel>().WithDetails("Invalid card number");
    }
}

public partial class IsInEnumRuleValidator : LiteValidator<FullTestModel>
{
    public IsInEnumRuleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Status).IsInEnum().WithDetails("Invalid status");
    }
}

public partial class PrecisionScaleValidator : LiteValidator<FullTestModel>
{
    public PrecisionScaleValidator(ValidationBuilder<FullTestModel> b) : base(b)
    {
        b.RuleFor(x => x.Price).PrecisionScale<FullTestModel>(5, 2).WithDetails("Price must have max 5 digits with 2 decimal places");
    }
}

// ── Tests ───────────────────────────────────────────────────────────────

public class BuiltInRulesTests
{
    private static ValidationBuilder<FullTestModel> B() => new();

    private static FullTestModel Default(
        string? name = "John", string? email = "john@test.com", string? card = null,
        int age = 25, decimal price = 9.99m, string? password = "pass",
        string? confirmPassword = "pass", TestEnum status = TestEnum.Active) =>
        new(name, email, card, age, price, password, confirmPassword, status);

    // ── Null ──
    [Fact] public void Null_NullValue_Passes() =>
        Assert.True(new NullRuleValidator(B()).Validate(Default(name: null)).IsSuccess);
    [Fact] public void Null_NonNullValue_Fails() =>
        Assert.False(new NullRuleValidator(B()).Validate(Default(name: "John")).IsSuccess);

    // ── Empty ──
    [Fact] public void Empty_NullValue_Passes() =>
        Assert.True(new EmptyRuleValidator(B()).Validate(Default(name: null)).IsSuccess);
    [Fact] public void Empty_EmptyString_Passes() =>
        Assert.True(new EmptyRuleValidator(B()).Validate(Default(name: "")).IsSuccess);
    [Fact] public void Empty_NonEmpty_Fails() =>
        Assert.False(new EmptyRuleValidator(B()).Validate(Default(name: "John")).IsSuccess);

    // ── Equal ──
    [Fact] public void Equal_MatchingValue_Passes() =>
        Assert.True(new EqualRuleValidator(B()).Validate(Default(age: 18)).IsSuccess);
    [Fact] public void Equal_NonMatchingValue_Fails()
    {
        var result = new EqualRuleValidator(B()).Validate(Default(age: 20));
        Assert.Contains(result.Errors, e => e.Details == "Must be 18");
    }
    [Fact] public void Equal_PasswordsMatch_Passes() =>
        Assert.True(new EqualRuleValidator(B()).Validate(Default(age: 18, password: "abc", confirmPassword: "abc")).IsSuccess);
    [Fact] public void Equal_PasswordsMismatch_Fails()
    {
        var result = new EqualRuleValidator(B()).Validate(Default(age: 18, password: "abc", confirmPassword: "xyz"));
        Assert.Contains(result.Errors, e => e.Details == "Passwords must match");
    }

    // ── NotEqual ──
    [Fact] public void NotEqual_DifferentValue_Passes() =>
        Assert.True(new NotEqualRuleValidator(B()).Validate(Default(age: 5)).IsSuccess);
    [Fact] public void NotEqual_SameValue_Fails() =>
        Assert.False(new NotEqualRuleValidator(B()).Validate(Default(age: 0)).IsSuccess);

    // ── Length ──
    [Fact] public void Length_InRange_Passes() =>
        Assert.True(new LengthRuleValidator(B()).Validate(Default(name: "John")).IsSuccess);
    [Fact] public void Length_TooShort_Fails()
    {
        var result = new LengthRuleValidator(B()).Validate(Default(name: "J"));
        Assert.False(result.IsSuccess);
    }
    [Fact] public void Length_TooLong_Fails()
    {
        var result = new LengthRuleValidator(B()).Validate(Default(name: new string('A', 51)));
        Assert.False(result.IsSuccess);
    }

    // ── Comparison ──
    [Fact] public void GreaterThan_Valid_Passes() =>
        Assert.True(new ComparisonRuleValidator(B()).Validate(Default(age: 25)).IsSuccess);
    [Fact] public void GreaterThan_Zero_Fails() =>
        Assert.False(new ComparisonRuleValidator(B()).Validate(Default(age: 0)).IsSuccess);
    [Fact] public void LessThan_TooHigh_Fails() =>
        Assert.False(new ComparisonRuleValidator(B()).Validate(Default(age: 200)).IsSuccess);

    // ── Between ──
    [Fact] public void InclusiveBetween_InRange_Passes() =>
        Assert.True(new BetweenRuleValidator(B()).Validate(Default(age: 75)).IsSuccess);
    [Fact] public void InclusiveBetween_Boundary_Passes() =>
        Assert.True(new BetweenRuleValidator(B()).Validate(Default(age: 1)).IsSuccess);
    [Fact] public void InclusiveBetween_OutOfRange_Fails() =>
        Assert.False(new BetweenRuleValidator(B()).Validate(Default(age: 0)).IsSuccess);
    [Fact] public void ExclusiveBetween_Boundary_Fails() =>
        Assert.False(new BetweenRuleValidator(B()).Validate(Default(age: 151)).IsSuccess);

    // ── Email ──
    [Fact] public void Email_Valid_Passes() =>
        Assert.True(new EmailRuleValidator(B()).Validate(Default(email: "user@example.com")).IsSuccess);
    [Fact] public void Email_NoAt_Fails() =>
        Assert.False(new EmailRuleValidator(B()).Validate(Default(email: "invalid")).IsSuccess);
    [Fact] public void Email_AtStart_Fails() =>
        Assert.False(new EmailRuleValidator(B()).Validate(Default(email: "@example.com")).IsSuccess);
    [Fact] public void Email_AtEnd_Fails() =>
        Assert.False(new EmailRuleValidator(B()).Validate(Default(email: "user@")).IsSuccess);

    // ── Matches (Regex) ──
    [Fact] public void Matches_Valid_Passes() =>
        Assert.True(new MatchesRuleValidator(B()).Validate(Default(name: "John")).IsSuccess);
    [Fact] public void Matches_Invalid_Fails() =>
        Assert.False(new MatchesRuleValidator(B()).Validate(Default(name: "john")).IsSuccess);

    // ── CreditCard ──
    [Fact] public void CreditCard_ValidLuhn_Passes() =>
        Assert.True(new CreditCardRuleValidator(B()).Validate(Default(card: "4111111111111111")).IsSuccess);
    [Fact] public void CreditCard_Invalid_Fails() =>
        Assert.False(new CreditCardRuleValidator(B()).Validate(Default(card: "1234567890123456")).IsSuccess);
    [Fact] public void CreditCard_WithSpaces_Passes() =>
        Assert.True(new CreditCardRuleValidator(B()).Validate(Default(card: "4111 1111 1111 1111")).IsSuccess);

    // ── IsInEnum ──
    [Fact] public void IsInEnum_ValidValue_Passes() =>
        Assert.True(new IsInEnumRuleValidator(B()).Validate(Default(status: TestEnum.Active)).IsSuccess);
    [Fact] public void IsInEnum_InvalidValue_Fails() =>
        Assert.False(new IsInEnumRuleValidator(B()).Validate(Default(status: (TestEnum)999)).IsSuccess);

    // ── PrecisionScale ──
    [Fact] public void PrecisionScale_Valid_Passes() =>
        Assert.True(new PrecisionScaleValidator(B()).Validate(Default(price: 123.45m)).IsSuccess);
    [Fact] public void PrecisionScale_TooManyDecimals_Fails() =>
        Assert.False(new PrecisionScaleValidator(B()).Validate(Default(price: 1.234m)).IsSuccess);
    [Fact] public void PrecisionScale_TooManyDigits_Fails() =>
        Assert.False(new PrecisionScaleValidator(B()).Validate(Default(price: 123456.78m)).IsSuccess);
}
