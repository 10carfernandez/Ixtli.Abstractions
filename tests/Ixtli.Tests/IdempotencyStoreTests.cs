#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

public class IdempotencyStoreTests
{
	[Fact]
	public async Task TryRecord_ReturnsTrue_WhenNew_AndFalse_WhenDuplicate_NotExpired()
	{
		var store = new InMemoryIdempotencyStore();
		var tenant = new TenantId(Guid.NewGuid());
		var key = new IdempotencyKey("charge:abc123");
		var ttl = DateTimeOffset.UtcNow.AddMinutes(5);

		(await store.TryRecordAsync(tenant, key, ttl)).Should().BeTrue();
		(await store.TryRecordAsync(tenant, key, ttl)).Should().BeFalse();
	}

	[Fact]
	public async Task TryRecord_TreatsExpiredAsNew()
	{
		var store = new InMemoryIdempotencyStore();
		var tenant = new TenantId(Guid.NewGuid());
		var key = new IdempotencyKey("transfer:xyz");
		var expired = DateTimeOffset.UtcNow.AddMinutes(-1);

		(await store.TryRecordAsync(tenant, key, expired)).Should().BeTrue();
		(await store.TryRecordAsync(tenant, key, DateTimeOffset.UtcNow.AddMinutes(10))).Should().BeTrue();
	}
}
