#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

// Original: :contentReference[oaicite:9]{index=9}
public class IdempotencyStoreTests
{
	[Fact]
	public async Task TryBegin_ReturnsReservation_WhenNew_And_Null_WhenDuplicate_NotExpired()
	{
		var store = new InMemoryIdempotencyStore();
		var tenant = new TenantId(Guid.NewGuid());
		var key = new IdempotencyKey("charge:abc123");
		var ttl = DateTimeOffset.UtcNow.AddMinutes(5);

		var r1 = await store.TryBeginAsync(tenant, key, ttl);
		r1.Should().NotBeNull();

		var r2 = await store.TryBeginAsync(tenant, key, ttl);
		r2.Should().BeNull();
	}

	[Fact]
	public async Task TryBegin_TreatsExpiredAsNew()
	{
		var store = new InMemoryIdempotencyStore();
		var tenant = new TenantId(Guid.NewGuid());
		var key = new IdempotencyKey("transfer:xyz");

		// first reservation with an already-expired TTL (simulate)
		var expired = DateTimeOffset.UtcNow.AddMinutes(-1);
		var r1 = await store.TryBeginAsync(tenant, key, expired);
		r1.Should().NotBeNull();

		// Now begin again with a future TTL — expired reservation should not block
		var r2 = await store.TryBeginAsync(tenant, key, DateTimeOffset.UtcNow.AddMinutes(10));
		r2.Should().NotBeNull();
	}

	[Fact]
	public async Task Commit_Allows_Replay()
	{
		var store = new InMemoryIdempotencyStore();
		var tenant = new TenantId(Guid.NewGuid());
		var key = new IdempotencyKey("commit:abc");

		var reservation = await store.TryBeginAsync(tenant, key, DateTimeOffset.UtcNow.AddMinutes(5));
		reservation.Should().NotBeNull();

		var headers = new System.Collections.Generic.Dictionary<string, string>
		{
			["X-Test"] = "yes"
		};
		var bodyBytes = System.Text.Encoding.UTF8.GetBytes("ok");

		await store.TryCommitAsync(tenant, key, 200, headers, new ReadOnlyMemory<byte>(bodyBytes));

		var replay = await store.TryGetReplayAsync(tenant, key);
		replay.Should().NotBeNull();
		replay!.HttpStatus.Should().Be(200);
		replay.Headers.Should().ContainKey("X-Test");
		System.Text.Encoding.UTF8.GetString(replay.Body).Should().Be("ok");
	}
}
