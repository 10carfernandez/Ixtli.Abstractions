# Ixtli.Abstractions
Means "face" or "eye" in Nahuatl, and is an abstractions package for authentication and authorization.

**Purpose:** Define contracts for authentication, authorization, rate limits, and idempotency.  

## Primary Concepts

- **Tenant**: the paying customer (org or user)
- **Plan**: bundle of limits & entitlements
- **API Key**: credential mapping to a tenant (storage outside)
- **Quota**: decision for "can this request run now?"
- **Idempotency**: dedupe for billable mutations

## Public Surface (v0.1)

- **Value Objects**: `TenantId`, `PlanId`, `ApiKeyId`, `IdempotencyKey`
- **DTOs**: `Tenant`, `Plan`, `Entitlement`, `RateLimitPolicy`, `RateLimitWindow`, `RequestDescriptor`, `QuotaDecision`, `ApiKeyValidationResult`
- **Interfaces**: `IApiKeyValidator`, `ITenantResolver`, `IPlanProvider`, `IQuotaEvaluator`, `IIdempotencyStore`, `IRequestSignatureVerifier`
- **Constants**: `HeaderNames`, `ErrorCodes`

## Sample sequence (validate → resolve → plan → quota → idempotency)

```csharp
public async Task<(bool Allowed, QuotaDecision? Quota, string? Error)> AuthorizeAsync(
    string presentedKey,
    RequestDescriptor request,
    Func<TenantId, IdempotencyKey?> idempotencyKeyProvider,
    CancellationToken ct)
{
    // 1) Validate API key
    var keyResult = await _apiKeyValidator.ValidateAsync(presentedKey, ct);
    if (!keyResult.Valid || keyResult.TenantId is null) return (false, null, ErrorCodes.Unauthorized);

    // 2) Resolve tenant
    var tenant = await _tenantResolver.GetTenantAsync(keyResult.TenantId.Value, ct);
    if (tenant is null || !tenant.Active) return (false, null, ErrorCodes.Forbidden);

    // 3) Load plan
    var plan = await _planProvider.GetPlanAsync(tenant.Id, ct);
    if (plan is null) return (false, null, ErrorCodes.Forbidden);

    // 4) Quota decision
    var quota = await _quotaEvaluator.CheckAsync(tenant.Id, plan, request, ct);
    if (!quota.Allowed) return (false, quota, ErrorCodes.QuotaExceeded);

    // 5) Idempotency (only if mutation & key present)
    var idemKey = idempotencyKeyProvider(tenant.Id);
    if (idemKey is not null)
    {
        var ttl = request.TimestampUtc.AddHours(24); // implementations can choose window
        var newEntry = await _idempotencyStore.TryRecordAsync(tenant.Id, idemKey.Value, ttl, ct);
        if (!newEntry) return (false, quota, ErrorCodes.ConflictIdempotency);
    }

    return (true, quota, null);
}
