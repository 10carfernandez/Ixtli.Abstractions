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
/// Supported signature algorithms for request signing.
/// </summary>
public enum SignatureAlg
{
	/// <summary>HMAC using SHA-256.</summary>
	HmacSha256,

	/// <summary>Ed25519 public-key signature.</summary>
	Ed25519
}

/// <summary>
/// Specification for how a request was signed so verifiers can canonicalize and check values consistently.
/// </summary>
/// <param name="Algorithm">The signature algorithm used.</param>
/// <param name="SignedHeaders">The list of header names (in order) that were included in the signature.</param>
/// <param name="Canonicalization">A human-readable description of canonicalization rules (informational).</param>
public sealed record SignatureSpec(SignatureAlg Algorithm, IReadOnlyList<string> SignedHeaders, string Canonicalization = "lowercase-headers;trim;single-space");

/// <summary>
/// A single entitlement (key/value) granted by a plan.
/// </summary>
public record Entitlement(string key, string value)
{
	/// <summary>Entitlement key (identifier).</summary>
	public string Key { get; init; } = key;

	/// <summary>Entitlement raw value.</summary>
	public string Value { get; init; } = value;

	/// <summary>Try to interpret the entitlement value as a boolean.</summary>
	public bool? AsBoolean()
	{
		if (bool.TryParse(Value, out var b)) return b;
		return null;
	}

	/// <summary>Try to interpret the entitlement value as an integer.</summary>
	public int? AsInt()
	{
		if (int.TryParse(Value, out var i)) return i;
		return null;
	}
}

/// <summary>
/// Rate limiting policy describing permits for a time window.
/// </summary>
public record RateLimitPolicy(int permitLimit, RateLimitWindow window, int? burst = null, double? refillPerSecond = null)
{
	/// <summary>Total allowed permits during the configured window (excluding burst).</summary>
	public int PermitLimit { get; init; } = permitLimit;

	/// <summary>The canonical window unit used for counting (Second/Minute/Hour/Day).</summary>
	public RateLimitWindow Window { get; init; } = window;

	/// <summary>Optional burst added to the permit limit allowing short spikes.</summary>
	public int? Burst { get; init; } = burst;

	/// <summary>
	/// Optional token-bucket refill rate (tokens per second). When present, evaluators may interpret limits
	/// using token-bucket semantics instead of fixed windows.
	/// </summary>
	public double? RefillPerSecond { get; init; } = refillPerSecond;
}

/// <summary>
/// A plan: bundle of limits and entitlements.
/// </summary>
public record Plan(PlanId id, string name, RateLimitPolicy rateLimit, IReadOnlyList<Entitlement> entitlements)
{
	/// <summary>Unique identifier for the plan.</summary>
	public PlanId Id { get; init; } = id;

	/// <summary>Human-readable plan name.</summary>
	public string Name { get; init; } = name;

	/// <summary>Rate limiting policy for the plan.</summary>
	public RateLimitPolicy RateLimit { get; init; } = rateLimit;

	/// <summary>List of entitlements included in the plan.</summary>
	public IReadOnlyList<Entitlement> Entitlements { get; init; } = entitlements;
}

/// <summary>
/// Tenant info (minimal). The paying customer (organization or user).
/// </summary>
public record Tenant(TenantId id, string name, PlanId planId, bool active)
{
	/// <summary>Tenant identifier.</summary>
	public TenantId Id { get; init; } = id;

	/// <summary>Display name for the tenant.</summary>
	public string Name { get; init; } = name;

	/// <summary>Plan identifier assigned to the tenant.</summary>
	public PlanId PlanId { get; init; } = planId;

	/// <summary>Whether the tenant is active (can be billed/served).</summary>
	public bool Active { get; init; } = active;
}

/// <summary>
/// Attribution information carried with requests to indicate payer/subject/sponsor context.
/// </summary>
public sealed record Attribution(TenantId payer, TenantId subject, string? sponsor = null, string actorType = "customer")
{
	/// <summary>Payer tenant identifier.</summary>
	public TenantId Payer { get; init; } = payer;

	/// <summary>Subject tenant identifier (who the action concerns).</summary>
	public TenantId Subject { get; init; } = subject;

	/// <summary>Optional sponsor identifier (free-form).</summary>
	public string? Sponsor { get; init; } = sponsor;

	/// <summary>Classification of the actor (e.g., "customer", "partner", "service:billing").</summary>
	public string ActorType { get; init; } = actorType;
}

/// <summary>
/// A minimal request descriptor used by quota evaluators (no HTTP types).
/// </summary>
public record RequestDescriptor(string method, string path, DateTimeOffset timestampUtc, string? endpointKey = null, decimal weight = 1m)
{
	/// <summary>HTTP method or logical action.</summary>
	public string Method { get; init; } = method;

	/// <summary>Request path or logical route.</summary>
	public string Path { get; init; } = path;

	/// <summary>Request timestamp (UTC) used for window alignment.</summary>
	public DateTimeOffset TimestampUtc { get; init; } = timestampUtc;

	/// <summary>Optional logical endpoint key (e.g., "charges.create").</summary>
	public string? EndpointKey { get; init; } = endpointKey;

	/// <summary>
	/// Weight (cost) of the request used for quota consumption and metering.
	/// Defaults to 1.0 for typical requests; fractional weights allow metered consumption.
	/// </summary>
	public decimal Weight { get; init; } = weight;
}

/// <summary>
/// Result of a quota evaluation for a request.
/// </summary>
public record QuotaDecision(bool allowed, int limit, int remaining, DateTimeOffset resetUtc, TimeSpan? retryAfter = null)
{
	/// <summary>True if the request is allowed at the time of evaluation.</summary>
	public bool Allowed { get; init; } = allowed;

	/// <summary>The effective limit for the current window (including burst if applicable).</summary>
	public int Limit { get; init; } = limit;

	/// <summary>Permits remaining after applying this request.</summary>
	public int Remaining { get; init; } = remaining;

	/// <summary>UTC time when the current window resets.</summary>
	public DateTimeOffset ResetUtc { get; init; } = resetUtc;

	/// <summary>Optional retry-after duration that callers can return to clients.</summary>
	public TimeSpan? RetryAfter { get; init; } = retryAfter;
}

/// <summary>
/// Result of API key validation.
/// </summary>
public record ApiKeyValidationResult(bool valid, TenantId? tenantId, ApiKeyId? keyId, string? reason)
{
	/// <summary>True when the presented key was valid.</summary>
	public bool Valid { get; init; } = valid;

	/// <summary>Tenant id mapped to the presented key (if valid).</summary>
	public TenantId? TenantId { get; init; } = tenantId;

	/// <summary>Internal id for the API key (implementation detail).</summary>
	public ApiKeyId? KeyId { get; init; } = keyId;

	/// <summary>Optional human-friendly reason for failure (diagnostics only).</summary>
	public string? Reason { get; init; } = reason;
}

/// <summary>
/// A token returned when an idempotency reservation wins the race. The caller should later commit the canonical response.
/// </summary>
public sealed record IdempotencyReservation(string reservationId, DateTimeOffset expiresAt)
{
	/// <summary>Opaque reservation identifier.</summary>
	public string ReservationId { get; init; } = reservationId;

	/// <summary>Expiration time for the reservation (UTC).</summary>
	public DateTimeOffset ExpiresAt { get; init; } = expiresAt;
}

/// <summary>
/// Represents a previously committed response for an idempotent key that can be replayed to duplicates.
/// </summary>
public sealed record IdempotencyReplay(int httpStatus, IReadOnlyDictionary<string, string> headers, byte[] body)
{
	/// <summary>HTTP status code of the saved response.</summary>
	public int HttpStatus { get; init; } = httpStatus;

	/// <summary>Headers that should be replayed to the duplicate request.</summary>
	public IReadOnlyDictionary<string, string> Headers { get; init; } = headers;

	/// <summary>Response body bytes to replay.</summary>
	public byte[] Body { get; init; } = body;
}
