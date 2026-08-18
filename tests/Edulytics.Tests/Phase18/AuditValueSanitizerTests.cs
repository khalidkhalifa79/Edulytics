using System.Text.Json;
using Edulytics.Services.Auditing;

namespace Edulytics.Tests.Phase18;

public sealed class AuditValueSanitizerTests
{
    [Fact]
    public void Serialize_RedactsSensitiveValuesRecursively()
    {
        var input =
            new Dictionary<string, object?>
            {
                ["email"] =
                    "person@example.com",
                ["password"] =
                    "DoNotPersistThis",
                ["passwordSetupToken"] =
                    "secret-token",
                ["nested"] =
                    new Dictionary<string, object?>
                    {
                        ["apiKey"] =
                            "secret-api-key",
                        ["safeValue"] =
                            "visible"
                    }
            };

        var json =
            AuditValueSanitizer.Serialize(
                input);

        Assert.DoesNotContain(
            "DoNotPersistThis",
            json,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "secret-token",
            json,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "secret-api-key",
            json,
            StringComparison.Ordinal);

        Assert.Contains(
            "person@example.com",
            json,
            StringComparison.Ordinal);

        Assert.Contains(
            "visible",
            json,
            StringComparison.Ordinal);

        using var document =
            JsonDocument.Parse(
                json);

        Assert.Equal(
            "[REDACTED]",
            document.RootElement
                .GetProperty("password")
                .GetString());

        Assert.Equal(
            "[REDACTED]",
            document.RootElement
                .GetProperty(
                    "passwordSetupToken")
                .GetString());

        Assert.Equal(
            "[REDACTED]",
            document.RootElement
                .GetProperty("nested")
                .GetProperty("apiKey")
                .GetString());
    }

    [Fact]
    public void Serialize_NullOrEmpty_ReturnsEmptyObject()
    {
        Assert.Equal(
            "{}",
            AuditValueSanitizer.Serialize(
                null));

        Assert.Equal(
            "{}",
            AuditValueSanitizer.Serialize(
                new Dictionary<string, object?>()));
    }
}
