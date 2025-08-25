#nullable enable
namespace Ixtli;

/// <summary>
/// Validates API keys and resolves their associated tenant and key information.
/// </summary>
public interface IApiKeyValidator
{
	/// <summary>
	/// Validates the presented API key and returns the validation result.
	/// </summary>
	/// <param name="presentedKey">The API key presented by the client.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The result of API key validation.</returns>
	Task<ApiKeyValidationResult> ValidateAsync(string presentedKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves tenant information given a tenant identifier.
/// </summary>
public interface ITenantResolver
{
	/// <summary>
	/// Retrieves the tenant details for the specified tenant identifier.
	/// </summary>
	/// <param name="tenantId">The unique identifier of the tenant.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The tenant details, or <c>null</c> if not found.</returns>
	Task<Tenant?> GetTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides plan information for a given tenant.
/// </summary>
public interface IPlanProvider
{
	/// <summary>
	/// Retrieves the plan assigned to the specified tenant.
	/// </summary>
	/// <param name="tenantId">The unique identifier of the tenant.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The plan details, or <c>null</c> if not found.</returns>
	Task<Plan?> GetPlanAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Evaluates quota for a tenant and plan, given a request descriptor.
/// </summary>
public interface IQuotaEvaluator
{
	/// <summary>
	/// Checks the quota for the specified tenant, plan, and request.
	/// </summary>
	/// <param name="tenant">The tenant identifier.</param>
	/// <param name="plan">The plan assigned to the tenant.</param>
	/// <param name="req">The request descriptor.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The quota decision for the request.</returns>
	Task<QuotaDecision> CheckAsync(TenantId tenant, Plan plan, RequestDescriptor req, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores and checks idempotency keys for deduplication of billable mutations.
/// </summary>
public interface IIdempotencyStore
{
	/// <summary>
	/// Attempts to record a new idempotency key for the specified tenant.
	/// </summary>
	/// <param name="tenant">The tenant identifier.</param>
	/// <param name="key">The idempotency key.</param>
	/// <param name="ttlUtc">The UTC expiration time for the key.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns><c>true</c> if the key was recorded (not a duplicate); otherwise, <c>false</c>.</returns>
	Task<bool> TryRecordAsync(TenantId tenant, IdempotencyKey key, DateTimeOffset ttlUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Verifies the signature of a request for authenticity and integrity.
/// </summary>
public interface IRequestSignatureVerifier
{
	/// <summary>
	/// Verifies the signature of a request using the provided API key ID, request body, and headers.
	/// </summary>
	/// <param name="keyId">The API key identifier.</param>
	/// <param name="body">The request body as a read-only memory buffer.</param>
	/// <param name="headers">The request headers.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns><c>true</c> if the signature is valid; otherwise, <c>false</c>.</returns>
	Task<bool> VerifyAsync(ApiKeyId keyId, ReadOnlyMemory<byte> body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
}
