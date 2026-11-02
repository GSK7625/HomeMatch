using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Http;

public enum IdentityJsonError { None, Invalid, TooLarge, UnsupportedMediaType }

/// <summary>Minimal API binds this parameter before calling the endpoint.</summary>
public sealed record IdentityBody<T>(T? Value, IdentityJsonError Error, int PropertyCount = 0) where T : class
{
    private const int MaximumBodyBytes = 16 * 1024;
    private static readonly JsonSerializerOptions StrictJson = new(JsonSerializerOptions.Strict)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 8
    };

    public static async ValueTask<IdentityBody<T>> BindAsync(HttpContext context, ParameterInfo _)
    {
        var request = context.Request;
        var allowEmpty = typeof(T) == typeof(EmptyIdentityRequest);
        if (request.ContentLength > MaximumBodyBytes) return new(null, IdentityJsonError.TooLarge);
        if (request.ContentLength == 0 && allowEmpty) return Empty();

        var validMedia = request.HasJsonContentType() && MediaTypeHeaderValue.TryParse(request.ContentType, out var media)
            && (string.IsNullOrEmpty(media.CharSet) || media.CharSet.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase));
        if (!validMedia && !allowEmpty) return new(null, IdentityJsonError.UnsupportedMediaType);

        var bytes = new byte[MaximumBodyBytes + 1];
        var count = 0;
        while (count < bytes.Length)
        {
            var read = await request.Body.ReadAsync(bytes.AsMemory(count), context.RequestAborted);
            if (read == 0) break;
            count += read;
        }
        if (count > MaximumBodyBytes) return new(null, IdentityJsonError.TooLarge);
        if (count == 0 && allowEmpty) return Empty();
        if (!validMedia) return new(null, IdentityJsonError.UnsupportedMediaType);

        try
        {
            using var document = JsonDocument.Parse(bytes.AsMemory(0, count),
                new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new(null, IdentityJsonError.Invalid);
            var value = document.RootElement.Deserialize<T>(StrictJson);
            return new(value, value is null ? IdentityJsonError.Invalid : IdentityJsonError.None,
                document.RootElement.EnumerateObject().Count());
        }
        catch (JsonException)
        {
            return new(null, IdentityJsonError.Invalid);
        }
    }

    private static IdentityBody<T> Empty() => new((T)(object)new EmptyIdentityRequest(), IdentityJsonError.None);
}

public sealed record EmptyIdentityRequest;
public sealed record VerifyEmailRequest(Guid ChallengeId, string Code);
