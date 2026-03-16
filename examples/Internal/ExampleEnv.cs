namespace Tokvera.Examples.Internal;

internal static class ExampleEnv
{
    public static string ApiKey()
        => Require("TOKVERA_API_KEY");

    public static string BaseUrl()
        => Environment.GetEnvironmentVariable("TOKVERA_INGEST_URL")
           ?? Environment.GetEnvironmentVariable("TOKVERA_API_BASE_URL")
           ?? "https://api.tokvera.org";

    public static string Feature(string fallback)
        => Environment.GetEnvironmentVariable("TOKVERA_FEATURE") ?? fallback;

    public static string RuntimeEnvironment()
        => Environment.GetEnvironmentVariable("TOKVERA_ENVIRONMENT") ?? "examples";

    public static string TenantId()
        => Environment.GetEnvironmentVariable("TOKVERA_TENANT_ID") ?? "tenant_demo_dotnet";

    private static string Require(string name)
        => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is required");
}
