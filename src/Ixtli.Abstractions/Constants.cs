#nullable enable
namespace Ixtli;

/// <summary>
/// Provides standard HTTP header names used throughout the API for authentication, idempotency, versioning, and rate limiting.
/// </summary>
public static class HeaderNames
{
	/// <summary>
	/// The header name for passing an API key.
	/// </summary>
	public const string ApiKey = "X-Api-Key";
	/// <summary>
	/// The standard HTTP Authorization header.
	/// </summary>
	public const string Authorization = "Authorization";
	/// <summary>
	/// The header name for passing a request signature.
	/// </summary>
	public const string Signature = "X-Signature";
	/// <summary>
	/// The header name for idempotency key, used to deduplicate requests.
	/// </summary>
	public const string IdempotencyKey = "Idempotency-Key";
	/// <summary>
	/// The header name for specifying the API version.
	/// </summary>
	public const string ApiVersion = "X-Api-Version";
	/// <summary>
	/// The header name for a unique request identifier.
	/// </summary>
	public const string RequestId = "X-Request-Id";
	/// <summary>
	/// The header name indicating the rate limit for the current window.
	/// </summary>
	public const string RateLimitLimit = "RateLimit-Limit";
	/// <summary>
	/// The header name indicating the number of remaining requests in the current window.
	/// </summary>
	public const string RateLimitRemaining = "RateLimit-Remaining";
	/// <summary>
	/// The header name indicating when the current rate limit window resets.
	/// </summary>
	public const string RateLimitReset = "RateLimit-Reset";
}

/// <summary>
/// Provides standard error codes used for API responses, representing common failure scenarios.
/// </summary>
public static class ErrorCodes
{
	/// <summary>
	/// Indicates that authentication failed due to missing or invalid credentials.
	/// </summary>
	public const string Unauthorized = "unauthorized";
	/// <summary>
	/// Indicates that the request is forbidden for the authenticated principal.
	/// </summary>
	public const string Forbidden = "forbidden";
	/// <summary>
	/// Indicates that the quota for the current window has been exceeded.
	/// </summary>
	public const string QuotaExceeded = "quota_exceeded";
	/// <summary>
	/// Indicates that the request signature is invalid.
	/// </summary>
	public const string InvalidSignature = "invalid_signature";
	/// <summary>
	/// Indicates a conflict due to a duplicate idempotency key.
	/// </summary>
	public const string ConflictIdempotency = "conflict_idempotency";
}
