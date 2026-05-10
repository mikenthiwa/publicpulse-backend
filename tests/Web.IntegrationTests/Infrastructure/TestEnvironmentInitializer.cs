using System.Runtime.CompilerServices;

namespace Web.IntegrationTests;

public static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            TestConnectionStrings.DefaultConnectionKey,
            TestConnectionStrings.DefaultConnection);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PublicPulse.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PublicPulse.Tests");
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            "public-pulse-tests-signing-key-with-enough-length");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
    }
}
