#nullable enable
namespace Ixtli;

/// <summary>
/// Validates a presented API key string and maps it to a tenant (and key id).
/// Storage, hashing, and revocation are implementation details elsewhere.
/// </summary>
public interface IApiKeyValidator
{
	/// <summary>
	/// Validates the presented API key and returns the validation result.
	/// </summary>
	/// <param name="presentedKey">The API key presented by the client.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<ApiKeyValidationResult> ValidateAsync(string presentedKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves tenant info by id (active state, display name, plan id).
/// </summary>
public interface ITenantResolver
{
	/// <summary>
	/// Retrieves tenant metadata for the provided tenant id.
	/// </summary>
	/// <param name="tenantId">Tenant identifier.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<Tenant?> GetTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the plan associated with a tenant.
/// </summary>
public interface IPlanProvider
{
	/// <summary>
	/// Loads the plan for the given tenant.
	/// </summary>
	/// <param name="tenantId">Tenant identifier.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<Plan?> GetPlanAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Computes a quota decision given a tenant, their plan, and a request descriptor.
/// Implementations may be fixed-window, sliding-window, token-bucket, etc.
/// </summary>
public interface IQuotaEvaluator
{
	/// <summary>
	/// Checks whether the request should be allowed and returns quota metadata.
	/// </summary>
	/// <param name="tenant">The tenant identifier.</param>
	/// <param name="plan">The plan assigned to the tenant.</param>
	/// <param name="req">The request descriptor.</param>
	/// <param name="weight">The weight (cost) of this request for metering. Defaults to 1.0.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<QuotaDecision> CheckAsync(TenantId tenant, Plan plan, RequestDescriptor req, decimal weight = 1m, CancellationToken cancellationToken = default);
}

/// <summary>
/// Records idempotency keys for billable mutations using a reserve/commit pattern.
/// </summary>
public interface IIdempotencyStore
{
	/// <summary>
	/// Attempts to reserve an idempotency key for the tenant. Returns a reservation when this caller won the race.
	/// </summary>
	/// <param name="tenant">Tenant identifier.</param>
	/// <param name="key">Idempotency key.</param>
	/// <param name="ttlUtc">Absolute expiration for the reservation.</param>
	/// <param name="fingerprintHash">Optional fingerprint of the request payload to detect differing requests reusing the same key.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<IdempotencyReservation?> TryBeginAsync(TenantId tenant, IdempotencyKey key, DateTimeOffset ttlUtc, string? fingerprintHash = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Commits the canonical response metadata for the reserved idempotency key so duplicates can replay the response.
	/// </summary>
	/// <param name="tenant">Tenant identifier.</param>
	/// <param name="key">Idempotency key.</param>
	/// <param name="httpStatus">HTTP status code of the canonical response.</param>
	/// <param name="headers">Headers associated with the canonical response.</param>
	/// <param name="body">Response body bytes.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task TryCommitAsync(TenantId tenant, IdempotencyKey key, int httpStatus, IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default);

	/// <summary>
	/// If a duplicate request arrives, attempt to retrieve a previously committed response for replay.
	/// </summary>
	/// <param name="tenant">Tenant identifier.</param>
	/// <param name="key">Idempotency key.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	Task<IdempotencyReplay?> TryGetReplayAsync(TenantId tenant, IdempotencyKey key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optionally verifies body or header signatures (e.g., HMAC). Implementations supply the crypto primitives.
/// </summary>
public interface IRequestSignatureVerifier
{
	/// <summary>
	/// Verifies that the provided signature spec and headers match the computed signature for the body and key.
	/// Implementations MUST validate a recent <see cref="HeaderNames.RequestTimestamp"/> to mitigate replay attacks.
	/// </summary>
	/// <param name="keyId">API key identifier for which to verify the signature.</param>
	/// <param name="body">Request body bytes.</param>
	/// <param name="headers">Headers as a map from header name to array of values (HTTP is multi-valued).</param>
	/// <param name="spec">Specification describing how the signature was constructed.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>True if signature is valid; otherwise false.</returns>
	Task<bool> VerifyAsync(ApiKeyId keyId, ReadOnlyMemory<byte> body, IReadOnlyDictionary<string, string[]> headers, SignatureSpec spec, CancellationToken cancellationToken = default);
}

/// <summary>
/// Small accessor for services that wish to expose the resolved Ixtli context to downstream code.
/// Implementations typically store this in an ambient context (e.g., per-request) owned by adapters.
/// </summary>
public interface IIxtliContextAccessor
{
	/// <summary>The resolved tenant (if any).</summary>
	Tenant Tenant { get; }

	/// <summary>The resolved plan for the tenant.</summary>
	Plan Plan { get; }

	/// <summary>The attribution information (payer/subject/sponsor).</summary>
	Attribution Attribution { get; }

	/// <summary>The last quota decision evaluated for the current request.</summary>
	QuotaDecision Quota { get; }
}
