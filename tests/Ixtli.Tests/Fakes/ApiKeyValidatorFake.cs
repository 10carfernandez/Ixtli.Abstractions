#nullable enable
namespace Ixtli.Tests.Fakes;

public sealed class ApiKeyValidatorFake : IApiKeyValidator
{
	private readonly IReadOnlyDictionary<string, (TenantId Tenant, ApiKeyId KeyId)> _map;

	public ApiKeyValidatorFake(IReadOnlyDictionary<string, (TenantId, ApiKeyId)> map)
		=> _map = map;

	public Task<ApiKeyValidationResult> ValidateAsync(string presentedKey, CancellationToken ct = default)
	{
		if (_map.TryGetValue(presentedKey, out var tuple))
		{
			return Task.FromResult(new ApiKeyValidationResult(
				valid: true,
				tenantId: tuple.Tenant,
				keyId: tuple.KeyId,
				reason: null));
		}

		return Task.FromResult(new ApiKeyValidationResult(
			valid: false,
			tenantId: null,
			keyId: null,
			reason: "unknown_key"));
	}
}
