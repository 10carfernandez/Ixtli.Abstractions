#nullable enable
using System.Collections.Concurrent;

namespace Ixtli.Tests.Fakes;

/// <summary>
/// In-memory idempotency store with reservation/commit/replay semantics.
/// Not for production; only to prove semantics for tests.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
	private readonly ConcurrentDictionary<(TenantId, string), IdempotencyReservation> _reservations = new();
	private readonly ConcurrentDictionary<(TenantId, string), IdempotencyReplay> _committed = new();

	public Task<IdempotencyReservation?> TryBeginAsync(TenantId tenant, IdempotencyKey key, DateTimeOffset ttlUtc, string? fingerprintHash = null, CancellationToken cancellationToken = default)
	{
		var mapKey = (tenant, key.Value);
		var now = DateTimeOffset.UtcNow;

		// If a committed response exists, the caller lost the race — return null (they should replay)
		if (_committed.TryGetValue(mapKey, out var committed))
			return Task.FromResult<IdempotencyReservation?>(null);

		while (true)
		{
			if (_reservations.TryGetValue(mapKey, out var existing))
			{
				// If reservation still valid -> lost race
				if (existing.ExpiresAt > now)
					return Task.FromResult<IdempotencyReservation?>(null);

				// expired -> attempt to remove and try again
				if (_reservations.TryRemove(mapKey, out _))
					continue;

				// retry in case of race
				continue;
			}

			var reservation = new IdempotencyReservation(Guid.NewGuid().ToString("N"), ttlUtc);
			if (_reservations.TryAdd(mapKey, reservation))
			{
				return Task.FromResult<IdempotencyReservation?>(reservation);
			}

			// race -> retry
		}
	}

	public Task TryCommitAsync(TenantId tenant, IdempotencyKey key, int httpStatus, IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
	{
		var mapKey = (tenant, key.Value);

		// Remove any reservation (best-effort)
		_reservations.TryRemove(mapKey, out _);

		var bodyArr = body.ToArray();
		var replay = new IdempotencyReplay(httpStatus, headers, bodyArr);
		_committed[mapKey] = replay;

		return Task.CompletedTask;
	}

	public Task<IdempotencyReplay?> TryGetReplayAsync(TenantId tenant, IdempotencyKey key, CancellationToken cancellationToken = default)
	{
		var mapKey = (tenant, key.Value);
		if (_committed.TryGetValue(mapKey, out var replay))
			return Task.FromResult<IdempotencyReplay?>(replay);

		return Task.FromResult<IdempotencyReplay?>(null);
	}
}
