#nullable enable
using FluentAssertions;
using Ixtli;
using Ixtli.Tests.Fakes;

public class ApiKeyValidatorTests
{
	[Fact]
	public async Task Validate_ReturnsTenant_And_KeyId_WhenKnown()
	{
		var tenant = new TenantId(Guid.NewGuid());
		var keyId = new ApiKeyId(Guid.NewGuid());

		var fake = new ApiKeyValidatorFake(new Dictionary<string, (TenantId, ApiKeyId)>
		{
			["alpha-key"] = (tenant, keyId)
		});

		var result = await fake.ValidateAsync("alpha-key");

		result.Valid.Should().BeTrue();
		result.TenantId.Should().Be(tenant);
		result.KeyId.Should().Be(keyId);
		result.Reason.Should().BeNull();
	}

	[Fact]
	public async Task Validate_Fails_WhenUnknown()
	{
		var fake = new ApiKeyValidatorFake(new Dictionary<string, (TenantId, ApiKeyId)>());

		var result = await fake.ValidateAsync("nope");

		result.Valid.Should().BeFalse();
		result.TenantId.Should().BeNull();
		result.KeyId.Should().BeNull();
		result.Reason.Should().NotBeNull();
	}
}
