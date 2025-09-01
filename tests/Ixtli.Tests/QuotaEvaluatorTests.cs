#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

// Original: :contentReference[oaicite:8]{index=8}
public class QuotaEvaluatorTests
{
	[Fact]
	public async Task FixedWindow_Minute_Should_Deny_OverLimit_And_Reset_NextWindow()
	{
		var tenant = new TenantId(Guid.NewGuid());
		var plan = new Plan(
			id: new PlanId(Guid.NewGuid()),
			name: "Basic",
			rateLimit: new RateLimitPolicy(permitLimit: 2, window: RateLimitWindow.Minute),
			entitlements: Array.Empty<Entitlement>());

		var evaluator = new FixedQuotaEvaluator();

		var t0 = new DateTimeOffset(2025, 1, 2, 03, 04, 05, TimeSpan.Zero);

		var r1 = await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/widgets", timestampUtc: t0));
		r1.Allowed.Should().BeTrue();
		r1.Remaining.Should().Be(1);

		var r2 = await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "POST", path: "/widgets", timestampUtc: t0.AddSeconds(10)));
		r2.Allowed.Should().BeTrue();
		r2.Remaining.Should().Be(0);

		var r3 = await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/widgets", timestampUtc: t0.AddSeconds(20)));
		r3.Allowed.Should().BeFalse();
		r3.Remaining.Should().Be(0);

		// Next minute -> resets
		var r4 = await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/widgets", timestampUtc: t0.AddMinutes(1)));
		r4.Allowed.Should().BeTrue();
		r4.Remaining.Should().Be(1);
	}

	[Fact]
	public async Task Burst_Increases_Capacity()
	{
		var tenant = new TenantId(Guid.NewGuid());
		var plan = new Plan(
			id: new PlanId(Guid.NewGuid()),
			name: "Pro",
			rateLimit: new RateLimitPolicy(permitLimit: 1, window: RateLimitWindow.Second, burst: 2),
			entitlements: Array.Empty<Entitlement>());

		var evaluator = new FixedQuotaEvaluator();

		var t0 = new DateTimeOffset(2025, 1, 2, 03, 04, 05, TimeSpan.Zero);

		// Limit 1 + Burst 2 = 3 allowed in the same second
		(await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/", timestampUtc: t0))).Allowed.Should().BeTrue();
		(await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/", timestampUtc: t0))).Allowed.Should().BeTrue();
		(await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/", timestampUtc: t0))).Allowed.Should().BeTrue();
		(await evaluator.CheckAsync(tenant, plan, new RequestDescriptor(method: "GET", path: "/", timestampUtc: t0))).Allowed.Should().BeFalse();
	}
}
