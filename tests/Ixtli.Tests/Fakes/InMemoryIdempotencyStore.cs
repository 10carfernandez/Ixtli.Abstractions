#nullable enable
using System.Collections.Concurrent;

namespace Ixtli.Tests.Fakes;

/// <summary>
/// In-memory idempotency store with absolute TTL timestamps.
/// Not for production; only to prove semantics.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
	private readonly ConcurrentDictionary<(TenantId, string), DateTimeOffset> _expirations = new();

	public Task<bool> TryRecordAsync(TenantId tenant, IdempotencyKey key, DateTimeOffset ttlUtc, CancellationToken ct = default)
	{
		var mapKey = (tenant, key.Value);

		while (true)
		{
			if (_expirations.TryGetValue(mapKey, out var existing))
			{
				if (existing > DateTimeOffset.UtcNow)
				{
					// Not expired yet -> duplicate
					return Task.FromResult(false);
				}

				// Expired -> attempt to update TTL (treat as new)
				if (_expirations.TryUpdate(mapKey, ttlUtc, existing))
					return Task.FromResult(true);

				continue; // retry on race
			}

			if (_expirations.TryAdd(mapKey, ttlUtc))
				return Task.FromResult(true);
		}
	}
}
