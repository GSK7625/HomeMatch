using HomeMatch.Modules.Identity.Infrastructure.Configuration;

namespace HomeMatch.UnitTests.Identity;

public class IdentityDatabaseConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Host=localhost;Username=homematch")]
    [InlineData("Database=homematch;Username=homematch")]
    [InlineData("Host=localhost;Database=homematch")]
    [InlineData("Host=localhost;Database=homematch;Username=homematch;Port=0")]
    [InlineData("private-invalid-connection-marker")]
    public void InvalidConnectionStringIsRejectedWithoutEchoingValues(string connection)
    {
        var result = new IdentityDatabaseOptionsValidator().Validate(null,
            new IdentityDatabaseOptions { DefaultConnection = connection });

        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:DefaultConnection", result.FailureMessage);
        Assert.DoesNotContain("private-invalid-connection-marker", result.FailureMessage);
    }

    [Fact]
    public void StructurallyValidConnectionDoesNotNeedANetworkConnectionToValidate()
    {
        var result = new IdentityDatabaseOptionsValidator().Validate(null,
            new IdentityDatabaseOptions { DefaultConnection = "Host=localhost;Port=55432;Database=homematch;Username=homematch" });

        Assert.True(result.Succeeded);
    }
}