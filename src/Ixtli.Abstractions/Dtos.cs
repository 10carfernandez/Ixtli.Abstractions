#nullable enable
namespace Ixtli;

/// <summary>
/// Represents the time window for a rate limit policy.
/// </summary>
public enum RateLimitWindow
{
	/// <summary>
	/// A one-second window.
	/// </summary>
	Second,
	/// <summary>
	/// A one-minute window.
	/// </summary>
	Minute,
	/// <summary>
	/// A one-hour window.
	/// </summary>
	Hour,
	/// <summary>
	/// A one-day window.
	/// </summary>
	Day
}
/// <summary>
/// Represents a named entitlement (feature or limit) granted to a tenant or plan.
/// </summary>
public record Entitlement
{
	/// <summary>
	/// The unique key identifying the entitlement (e.g., "maxProjects").
	/// </summary>
	public string Key { get; init; }

	/// <summary>
	/// The value associated with the entitlement (e.g., "10").
	/// </summary>
	public string Value { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Entitlement"/> record.
	/// </summary>
	/// <param name="key">The unique key for the entitlement.</param>
	/// <param name="value">The value of the entitlement.</param>
	public Entitlement(string key, string value)
	{
		Key = key;
		Value = value;
	}
}

/// <summary>
/// Defines the rate limiting policy for a plan or tenant.
/// </summary>
public record RateLimitPolicy
{
	/// <summary>
	/// The maximum number of permitted actions within the specified window.
	/// </summary>
	public int PermitLimit { get; init; }

	/// <summary>
	/// The time window over which the permit limit applies.
	/// </summary>
	public RateLimitWindow Window { get; init; }

	/// <summary>
	/// The optional burst size allowed above the steady-state limit.
	/// </summary>
	public int? Burst { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RateLimitPolicy"/> record.
	/// </summary>
	/// <param name="permitLimit">The maximum number of permitted actions.</param>
	/// <param name="window">The time window for the rate limit.</param>
	/// <param name="burst">The optional burst size.</param>
	public RateLimitPolicy(int permitLimit, RateLimitWindow window, int? burst = null)
	{
		PermitLimit = permitLimit;
		Window = window;
		Burst = burst;
	}
}

/// <summary>
/// Represents a subscription plan, including its rate limit policy and entitlements.
/// </summary>
public record Plan
{
	/// <summary>
	/// The unique identifier for the plan.
	/// </summary>
	public PlanId Id { get; init; }

	/// <summary>
	/// The display name of the plan.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// The rate limit policy associated with the plan.
	/// </summary>
	public RateLimitPolicy RateLimit { get; init; }

	/// <summary>
	/// The entitlements granted by the plan.
	/// </summary>
	public IReadOnlyList<Entitlement> Entitlements { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Plan"/> record.
	/// </summary>
	/// <param name="id">The unique identifier for the plan.</param>
	/// <param name="name">The display name of the plan.</param>
	/// <param name="rateLimit">The rate limit policy for the plan.</param>
	/// <param name="entitlements">The entitlements granted by the plan.</param>
	public Plan(PlanId id, string name, RateLimitPolicy rateLimit, IReadOnlyList<Entitlement> entitlements)
	{
		Id = id;
		Name = name;
		RateLimit = rateLimit;
		Entitlements = entitlements;
	}
}

/// <summary>
/// Represents a tenant (customer or organization) in the system.
/// </summary>
public record Tenant
{
	/// <summary>
	/// The unique identifier for the tenant.
	/// </summary>
	public TenantId Id { get; init; }

	/// <summary>
	/// The display name of the tenant.
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// The identifier of the plan assigned to the tenant.
	/// </summary>
	public PlanId PlanId { get; init; }

	/// <summary>
	/// Indicates whether the tenant is active and allowed to use the system.
	/// </summary>
	public bool Active { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Tenant"/> record.
	/// </summary>
	/// <param name="id">The unique identifier for the tenant.</param>
	/// <param name="name">The display name of the tenant.</param>
	/// <param name="planId">The identifier of the plan assigned to the tenant.</param>
	/// <param name="active">Whether the tenant is active.</param>
	public Tenant(TenantId id, string name, PlanId planId, bool active)
	{
		Id = id;
		Name = name;
		PlanId = planId;
		Active = active;
	}
}

/// <summary>
/// Minimal request descriptor for quota decisions. No HTTP types on purpose.
/// </summary>
public record RequestDescriptor
{
	/// <summary>
	/// The logical method or verb of the request (e.g., "POST", "GET").
	/// </summary>
	public string Method { get; init; }

	/// <summary>
	/// The logical path or resource identifier for the request.
	/// </summary>
	public string Path { get; init; }

	/// <summary>
	/// The UTC timestamp when the request was made.
	/// </summary>
	public DateTimeOffset TimestampUtc { get; init; }

	/// <summary>
	/// An optional logical endpoint key for advanced routing or quota partitioning.
	/// </summary>
	public string? EndpointKey { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RequestDescriptor"/> record.
	/// </summary>
	/// <param name="method">The logical method or verb of the request.</param>
	/// <param name="path">The logical path or resource identifier.</param>
	/// <param name="timestampUtc">The UTC timestamp of the request.</param>
	/// <param name="endpointKey">An optional logical endpoint key.</param>
	public RequestDescriptor(string method, string path, DateTimeOffset timestampUtc, string? endpointKey = null)
	{
		Method = method;
		Path = path;
		TimestampUtc = timestampUtc;
		EndpointKey = endpointKey;
	}
}

/// <summary>
/// Result of a quota evaluation for the current window.
/// </summary>
public record QuotaDecision
{
	/// <summary>
	/// Indicates whether the request is allowed under the current quota.
	/// </summary>
	public bool Allowed { get; init; }

	/// <summary>
	/// The maximum number of permitted actions in the current window.
	/// </summary>
	public int Limit { get; init; }

	/// <summary>
	/// The number of remaining permitted actions in the current window.
	/// </summary>
	public int Remaining { get; init; }

	/// <summary>
	/// The UTC timestamp when the current quota window resets.
	/// </summary>
	public DateTimeOffset ResetUtc { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="QuotaDecision"/> record.
	/// </summary>
	/// <param name="allowed">Whether the request is allowed.</param>
	/// <param name="limit">The maximum number of permitted actions.</param>
	/// <param name="remaining">The number of remaining permitted actions.</param>
	/// <param name="resetUtc">The UTC timestamp when the quota resets.</param>
	public QuotaDecision(bool allowed, int limit, int remaining, DateTimeOffset resetUtc)
	{
		Allowed = allowed;
		Limit = limit;
		Remaining = remaining;
		ResetUtc = resetUtc;
	}
}

/// <summary>
/// Result of API key validation. Reason is a human-friendly string only for diagnostics.
/// </summary>
public record ApiKeyValidationResult
{
	/// <summary>
	/// Indicates whether the API key is valid.
	/// </summary>
	public bool Valid { get; init; }

	/// <summary>
	/// The tenant ID associated with the API key, if valid.
	/// </summary>
	public TenantId? TenantId { get; init; }

	/// <summary>
	/// The API key ID, if available.
	/// </summary>
	public ApiKeyId? KeyId { get; init; }

	/// <summary>
	/// A human-friendly reason for the validation result, for diagnostics.
	/// </summary>
	public string? Reason { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ApiKeyValidationResult"/> record.
	/// </summary>
	/// <param name="valid">Whether the API key is valid.</param>
	/// <param name="tenantId">The tenant ID associated with the key.</param>
	/// <param name="keyId">The API key ID.</param>
	/// <param name="reason">A human-friendly reason for diagnostics.</param>
	public ApiKeyValidationResult(bool valid, TenantId? tenantId, ApiKeyId? keyId, string? reason)
	{
		Valid = valid;
		TenantId = tenantId;
		KeyId = keyId;
		Reason = reason;
	}
}
