using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Refresh;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Presentation.Http;

/// <summary>Per-instance quotas for routes that need an atomic IP and subject decision.</summary>
public sealed class IdentityQuotas(IOptions<JwtOptions> options, TimeProvider clock) : IRefreshRequestQuota
{
    private readonly AtomicRequestQuota _login = new(options, clock, "HomeMatch.Identity.LoginQuota.v1",
        10, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(1));
    private readonly AtomicRequestQuota _registration = new(options, clock, "HomeMatch.Identity.RegistrationQuota.v1",
        5, TimeSpan.FromMinutes(15), 3, TimeSpan.FromHours(1));
    private readonly AtomicRequestQuota _refresh = new(options, clock, "HomeMatch.Identity.RefreshQuota.v1",
        120, TimeSpan.FromMinutes(1), 30, TimeSpan.FromMinutes(1));
    private readonly AtomicRequestQuota _logoutCurrent = new(options, clock, "HomeMatch.Identity.LogoutQuota.v1",
        30, TimeSpan.FromMinutes(1), 1, TimeSpan.FromMinutes(1));
    private readonly AtomicRequestQuota _logoutAll = new(options, clock, "HomeMatch.Identity.LogoutAllQuota.v1",
        120, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(15));
    private readonly AtomicRequestQuota _otpSend = new(options, clock, "public-otp-send",
        30, TimeSpan.FromMinutes(1), 5, TimeSpan.FromMinutes(15));
    private readonly AtomicRequestQuota _otpVerify = new(options, clock, "public-otp-verify",
        60, TimeSpan.FromMinutes(1), 20, TimeSpan.FromMinutes(15));

    public TimeSpan? TryLogin(string ip, string? email) => _login.TryAcquire(ip, email);
    public TimeSpan? TryRegistration(string ip, string? email) => _registration.TryAcquire(ip, email);
    public TimeSpan? TryAcquire(string ip, Guid? sessionId) => _refresh.TryAcquire(ip, sessionId?.ToString("D"));
    public TimeSpan? TryLogoutCurrent(string ip) => _logoutCurrent.TryAcquire(ip, null);
    public TimeSpan? TryLogoutAll(string ip, long userId) => _logoutAll.TryAcquire(ip, userId.ToString("D"));
    public TimeSpan? TryPublicOtp(string ip, string? subject, bool sending)
        => (sending ? _otpSend : _otpVerify).TryAcquire(ip, subject);
}

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
