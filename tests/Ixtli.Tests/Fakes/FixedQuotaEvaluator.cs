#nullable enable
using System.Collections.Concurrent;

namespace Ixtli.Tests.Fakes;

/// <summary>
/// Minimal fixed-window rate limiter to exercise the abstraction:
/// - Window boundaries are aligned to the timestamp supplied in RequestDescriptor.
/// - Burst, if present, increases the per-window capacity by Burst.
/// This is intentionally simple and stateful for test purposes only.
/// </summary>
public sealed class FixedQuotaEvaluator : IQuotaEvaluator
{
	private readonly ConcurrentDictionary<(TenantId Tenant, DateTimeOffset WindowStart), int> _counts = new();

	public Task<QuotaDecision> CheckAsync(TenantId tenant, Plan plan, RequestDescriptor req, decimal weight = 1m, CancellationToken cancellationToken = default)
	{
		var (windowStart, windowEnd) = GetWindowBounds(req.TimestampUtc, plan.RateLimit.Window);
		int limit = plan.RateLimit.PermitLimit + (plan.RateLimit.Burst ?? 0);

		var key = (tenant, windowStart);
		int current = _counts.GetOrAdd(key, 0);

		bool allowed = current < limit;
		int nextCount = allowed ? current + 1 : current;
		_counts[key] = nextCount;

		int remaining = Math.Max(0, limit - nextCount);
		var decision = new QuotaDecision(allowed, limit, remaining, windowEnd);
		return Task.FromResult(decision);
	}

	private static (DateTimeOffset start, DateTimeOffset end) GetWindowBounds(DateTimeOffset ts, RateLimitWindow window)
	{
		DateTimeOffset start = window switch
		{
			RateLimitWindow.Second => new DateTimeOffset(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, TimeSpan.Zero),
			RateLimitWindow.Minute => new DateTimeOffset(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, TimeSpan.Zero),
			RateLimitWindow.Hour => new DateTimeOffset(ts.Year, ts.Month, ts.Day, ts.Hour, 0, 0, TimeSpan.Zero),
			RateLimitWindow.Day => new DateTimeOffset(ts.Year, ts.Month, ts.Day, 0, 0, 0, TimeSpan.Zero),
			_ => throw new ArgumentOutOfRangeException(nameof(window))
		};

		DateTimeOffset end = window switch
		{
			RateLimitWindow.Second => start.AddSeconds(1),
			RateLimitWindow.Minute => start.AddMinutes(1),
			RateLimitWindow.Hour => start.AddHours(1),
			RateLimitWindow.Day => start.AddDays(1),
			_ => throw new ArgumentOutOfRangeException(nameof(window))
		};

		return (start, end);
	}
}
