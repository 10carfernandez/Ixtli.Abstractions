#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

// Original: :contentReference[oaicite:10]{index=10}
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
		var plan = new Plan(planId, "Starter", new RateLimitPolicy(2, RateLimitWindow.Minute), Array.Empty<Entitlement>());

		// 4) Quota check
		var evaluator = new FixedQuotaEvaluator();
		var now = DateTimeOffset.UtcNow;

		var q1 = await evaluator.CheckAsync(tenantDto.Id, plan, new RequestDescriptor(method: "POST", path: "/charges", timestampUtc: now, endpointKey: "charges.create"));
		q1.Allowed.Should().BeTrue();

		// 5) Idempotency reserve -> commit -> replay
		var store = new InMemoryIdempotencyStore();
		var idKey = new IdempotencyKey("charges:create:abc");

		var reservation = await store.TryBeginAsync(tenant, idKey, now.AddMinutes(10));
		reservation.Should().NotBeNull();

		var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
		var body = System.Text.Encoding.UTF8.GetBytes("{\"ok\":true}");

		await store.TryCommitAsync(tenant, idKey, 201, headers, new ReadOnlyMemory<byte>(body));

		var replay = await store.TryGetReplayAsync(tenant, idKey);
		replay.Should().NotBeNull();
		replay!.HttpStatus.Should().Be(201);
	}
}
