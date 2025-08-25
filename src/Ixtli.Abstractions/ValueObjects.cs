#nullable enable
namespace Ixtli;

/// <summary>
/// Represents the unique identifier for a paying customer (organization or user).
/// </summary>
public readonly record struct TenantId
{
	/// <summary>
	/// The underlying GUID value of the tenant identifier.
	/// </summary>
	public Guid Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TenantId"/> struct.
	/// </summary>
	/// <param name="value">The GUID value representing the tenant.</param>
	public TenantId(Guid value) => Value = value;

	/// <inheritdoc/>
	public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents the unique identifier for a plan (bundle of limits/entitlements).
/// </summary>
public readonly record struct PlanId
{
	/// <summary>
	/// The underlying GUID value of the plan identifier.
	/// </summary>
	public Guid Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PlanId"/> struct.
	/// </summary>
	/// <param name="value">The GUID value representing the plan.</param>
	public PlanId(Guid value) => Value = value;

	/// <inheritdoc/>
	public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents the unique identifier for an API key credential (hash &amp; storage external).
/// </summary>
public readonly record struct ApiKeyId
{
	/// <summary>
	/// The underlying GUID value of the API key identifier.
	/// </summary>
	public Guid Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ApiKeyId"/> struct.
	/// </summary>
	/// <param name="value">The GUID value representing the API key.</param>
	public ApiKeyId(Guid value) => Value = value;

	/// <inheritdoc/>
	public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents a deduplication key for billable mutations (idempotency).
/// </summary>
public readonly record struct IdempotencyKey
{
	/// <summary>
	/// The string value of the idempotency key.
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="IdempotencyKey"/> struct.
	/// </summary>
	/// <param name="value">The string value representing the idempotency key.</param>
	public IdempotencyKey(string value) => Value = value;

	/// <inheritdoc/>
	public override string ToString() => Value;
}
