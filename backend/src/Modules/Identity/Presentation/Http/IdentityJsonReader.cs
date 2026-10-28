using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Http;

public enum IdentityJsonError { None, Invalid, TooLarge, UnsupportedMediaType }
public sealed record IdentityJsonResult<T>(T? Value, IdentityJsonError Error) where T : class;

/// <summary>Bounded UTF-8 object reader shared by Identity endpoints; parsing delegates must not retain JsonElements.</summary>
public static class IdentityJsonReader
{
    public const int MaximumBodyBytes = 16 * 1024;

    public static Task<IdentityJsonResult<T>> ReadAsync<T>(HttpRequest request, Func<JsonElement, T?> parse,
        CancellationToken cancellationToken) where T : class
        => ReadAsync(request, parse, cancellationToken, emptyValue: null);

    public static async Task<IdentityJsonResult<T>> ReadAsync<T>(HttpRequest request, Func<JsonElement, T?> parse,
        CancellationToken cancellationToken, T? emptyValue) where T : class
    {
        if (request.ContentLength > MaximumBodyBytes) return new(null, IdentityJsonError.TooLarge);
        if (request.ContentLength == 0 && emptyValue is not null) return new(emptyValue, IdentityJsonError.None);
        var validMedia = request.HasJsonContentType() && MediaTypeHeaderValue.TryParse(request.ContentType, out var media)
            && (string.IsNullOrEmpty(media.CharSet) || media.CharSet.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase));
        if (!validMedia && emptyValue is null) return new(null, IdentityJsonError.UnsupportedMediaType);
        var bytes = new byte[MaximumBodyBytes + 1];
        var count = 0;
        while (count < bytes.Length)
        {
            var read = await request.Body.ReadAsync(bytes.AsMemory(count), cancellationToken);
            if (read == 0) break;
            count += read;
        }
        if (count > MaximumBodyBytes) return new(null, IdentityJsonError.TooLarge);
        if (count == 0 && emptyValue is not null) return new(emptyValue, IdentityJsonError.None);
        if (!validMedia) return new(null, IdentityJsonError.UnsupportedMediaType);
        try
        {
            using var document = JsonDocument.Parse(bytes.AsMemory(0, count), new JsonDocumentOptions { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new(null, IdentityJsonError.Invalid);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
                if (!names.Add(property.Name)) return new(null, IdentityJsonError.Invalid);
            var value = parse(document.RootElement);
            return new(value, value is null ? IdentityJsonError.Invalid : IdentityJsonError.None);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return new(null, IdentityJsonError.Invalid);
        }
    }
}
