using System.Text.Json;
using System.Text.Json.Nodes;

namespace Edulytics.Services.Auditing;

public static class AuditValueSanitizer
{
    private static readonly string[] SensitiveFragments =
    [
        "password",
        "token",
        "secret",
        "apikey",
        "api_key",
        "authorization",
        "cookie",
        "connectionstring",
        "securitystamp",
        "clientsecret",
        "credential",
        "privatekey",
        "filebytes",
        "rawfile",
        "contentbytes"
    ];

    private static readonly JsonSerializerOptions Options =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

    public static string Serialize(
        IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null ||
            values.Count == 0)
        {
            return "{}";
        }

        var node =
            JsonSerializer.SerializeToNode(
                values,
                Options);

        Redact(
            node);

        return node?.ToJsonString(
                   Options)
               ?? "{}";
    }

    private static void Redact(
        JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var keys =
                obj.Select(x => x.Key)
                    .ToArray();

            foreach (var key in keys)
            {
                if (IsSensitive(key))
                {
                    obj[key] =
                        "[REDACTED]";
                }
                else
                {
                    Redact(
                        obj[key]);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                Redact(
                    item);
            }
        }
    }

    private static bool IsSensitive(
        string propertyName)
    {
        var normalized =
            propertyName
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Trim()
                .ToLowerInvariant();

        return SensitiveFragments.Any(
            fragment =>
                normalized.Contains(
                    fragment
                        .Replace("_", string.Empty),
                    StringComparison.Ordinal));
    }
}
