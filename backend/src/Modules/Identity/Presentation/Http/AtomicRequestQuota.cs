using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Http;

/// <summary>One-instance atomic acquisition of independent IP/subject windows; keys contain no plaintext identity.</summary>
internal sealed class AtomicRequestQuota(IOptions<JwtOptions> options, TimeProvider clock, string purpose,
    int ipLimit, TimeSpan ipDuration, int subjectLimit, TimeSpan subjectDuration)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Window> _windows = [];
    private readonly byte[] _key = HMACSHA256.HashData(Convert.FromBase64String(options.Value.Secret), Encoding.UTF8.GetBytes(purpose));
    private DateTimeOffset _nextCleanup;

    public TimeSpan? TryAcquire(string clientAddress, string? subjectKey)
    {
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            if (now >= _nextCleanup)
            {
                foreach (var key in _windows.Where(pair => pair.Value.End <= now).Select(pair => pair.Key).ToArray())
                    _windows.Remove(key);
                _nextCleanup = now.AddMinutes(1);
            }
            var ip = GetWindow("ip:" + clientAddress, now, ipDuration, ipLimit);
            var subject = subjectKey is null ? null : GetWindow("subject:" + subjectKey, now, subjectDuration, subjectLimit);
            var retry = TimeSpan.Zero;
            if (ip.Count >= ip.Limit) retry = ip.End - now;
            if (subject is not null && subject.Count >= subject.Limit && subject.End - now > retry) retry = subject.End - now;
            if (retry > TimeSpan.Zero) return retry;
            ip.Count++;
            if (subject is not null) subject.Count++;
            return null;
        }
    }

    private Window GetWindow(string input, DateTimeOffset now, TimeSpan duration, int limit)
    {
        var key = Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(input)));
        if (!_windows.TryGetValue(key, out var window) || window.End <= now)
            _windows[key] = window = new Window(now + duration, limit);
        return window;
    }

    private sealed class Window(DateTimeOffset end, int limit)
    {
        public DateTimeOffset End { get; } = end;
        public int Limit { get; } = limit;
        public int Count { get; set; }
    }
}
