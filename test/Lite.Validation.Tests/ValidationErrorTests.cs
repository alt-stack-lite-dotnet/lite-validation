using Lite.Validation;

namespace Lite.Validation.Tests;

public class ValidationErrorTests
{
    [Fact]
    public void Ctor_StoresPropertyNameAndDetails()
    {
        var err = new ValidationError("Email", "Invalid format");
        Assert.Equal("Email", err.PropertyName);
        Assert.Equal("Invalid format", err.Details);
    }
}
