#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

public class SequenceHappyPathTests
{
	[Fact]
	public async Task Validate_Resolve_Plan_Quota_Idempotency()
	{
		// 1) API key validation
		var tenant = new TenantId(Guid.NewGuid());
		var keyId = new ApiKeyId(Guid.NewGuid());
		var keyMap = new Dictionary<string, (TenantId, ApiKeyId)> { ["alpha-key"] = (tenant, keyId) };
		var keyValidator = new ApiKeyValidatorFake(keyMap);

		var keyResult = await keyValidator.ValidateAsync("alpha-key");
		keyResult.Valid.Should().BeTrue();
		keyResult.TenantId.Should().Be(tenant);
		keyResult.KeyId.Should().Be(keyId);

		// 2) Tenant info (pretend we fetched it)
		var planId = new PlanId(Guid.NewGuid());
		var tenantDto = new Tenant(id: tenant, name: "Acme Co", planId: planId, active: true);

		// 3) Plan info (pretend we fetched it)
		var plan = new Plan(
			id: planId,
			name: "Starter",
			rateLimit: new RateLimitPolicy(permitLimit: 2, window: RateLimitWindow.Minute),
			entitlements: Array.Empty<Entitlement>());

		// 4) Quota check
		var evaluator = new FixedQuotaEvaluator();
		var now = DateTimeOffset.UtcNow;

		var q1 = await evaluator.CheckAsync(tenantDto.Id, plan, new RequestDescriptor(method: "POST", path: "/charges", timestampUtc: now, endpointKey: "charges.create"));
		q1.Allowed.Should().BeTrue();

		// 5) Idempotency record (first time ok; duplicate denied by store)
		var store = new InMemoryIdempotencyStore();
		var idKey = new IdempotencyKey("charges:create:abc");

		var first = await store.TryRecordAsync(tenant, idKey, now.AddMinutes(10));
		first.Should().BeTrue();

		var dup = await store.TryRecordAsync(tenant, idKey, now.AddMinutes(10));
		dup.Should().BeFalse();
	}
}
