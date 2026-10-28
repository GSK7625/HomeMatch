namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>Persisted fixed window. Callers must lock the row before checking or consuming a permit.</summary>
public sealed class EmailVerificationQuota
{
    private EmailVerificationQuota() { }
    public EmailVerificationQuota(string key, DateTimeOffset start, DateTimeOffset end)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 160 || end <= start
            || start.Offset != TimeSpan.Zero || end.Offset != TimeSpan.Zero) throw new ArgumentException("Invalid quota window.");
        Key = key; WindowStart = start; WindowEnd = end;
    }
    public string Key { get; private set; } = string.Empty;
    public DateTimeOffset WindowStart { get; private set; }
    public DateTimeOffset WindowEnd { get; private set; }
    public int Count { get; private set; }

    public void RenewIfExpired(DateTimeOffset now, TimeSpan duration, bool utcDay = false)
    {
        if (now.Offset != TimeSpan.Zero || duration <= TimeSpan.Zero) throw new ArgumentException("Invalid quota time.");
        if (now < WindowEnd) return;
        WindowStart = utcDay ? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero) : now;
        WindowEnd = WindowStart + duration; Count = 0;
    }

    public TimeSpan? RetryAfter(DateTimeOffset now, int limit)
        => Count >= limit && now < WindowEnd ? WindowEnd - now : null;

    public void Consume(DateTimeOffset now, int limit)
    {
        if (now < WindowStart || now >= WindowEnd || Count >= limit) throw new InvalidOperationException("Quota permit is unavailable.");
        Count++;
    }
}
