using Guild.Domain.Results;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Domain;

public sealed class GuildTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public void Create_HappyPath_SeedsEveryoneAndAdministratorRoles()
	{
		var result = GuildEntity.Create(
			id: 1,
			name: "Test",
			description: null,
			iconUrl: null,
			bannerUrl: null,
			ownerId: 42,
			everyoneRoleId: 2,
			adminRoleId: 3,
			now: Now);

		Assert.True(result.Succeeded);
		var guild = result.Value;

		Assert.Equal(2, guild.Roles.Count);

		var everyone = Assert.Single(guild.Roles, r => r.IsDefault);
		Assert.Equal("@everyone", everyone.Name);
		Assert.Equal(515L, everyone.Permissions);
		Assert.Equal(0, everyone.Position);

		var admin = Assert.Single(guild.Roles, r => !r.IsDefault);
		Assert.Equal("Administrator", admin.Name);
		Assert.Equal(256L, admin.Permissions);
		Assert.Equal(1, admin.Position);

		var ownerMember = Assert.Single(guild.Members);
		Assert.Equal(42L, ownerMember.UserId);

		var memberRole = Assert.Single(guild.MemberRoles);
		Assert.Equal(admin.Id, memberRole.RoleId);
		Assert.Equal(42L, memberRole.UserId);
	}

	[Fact]
	public void Create_EmptyName_Fails()
	{
		var result = GuildEntity.Create(
			id: 1, name: "", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 42, everyoneRoleId: 2, adminRoleId: 3, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildNameRequired, result.Error);
	}

	[Fact]
	public void Create_NameTooLong_Fails()
	{
		var name = new string('a', 101);

		var result = GuildEntity.Create(
			id: 1, name: name, description: null, iconUrl: null, bannerUrl: null,
			ownerId: 42, everyoneRoleId: 2, adminRoleId: 3, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildNameTooLong, result.Error);
	}

	[Fact]
	public void Create_IconUrlTooLong_Fails()
	{
		var iconUrl = new string('a', 513);

		var result = GuildEntity.Create(
			id: 1, name: "Test", description: null, iconUrl: iconUrl, bannerUrl: null,
			ownerId: 42, everyoneRoleId: 2, adminRoleId: 3, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildIconUrlTooLong, result.Error);
	}

	[Fact]
	public void Create_BannerUrlTooLong_Fails()
	{
		var bannerUrl = new string('a', 513);

		var result = GuildEntity.Create(
			id: 1, name: "Test", description: null, iconUrl: null, bannerUrl: bannerUrl,
			ownerId: 42, everyoneRoleId: 2, adminRoleId: 3, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildBannerUrlTooLong, result.Error);
	}

	[Fact]
	public void UpdateSettings_NullName_KeepsExistingName()
	{
		var guild = CreateValid();
		var originalName = guild.Name;

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal(originalName, guild.Name);
	}

	[Fact]
	public void UpdateSettings_EmptyDescription_ClearsDescription()
	{
		var guild = CreateValid(description: "previous");

		var result = guild.UpdateSettings(
			name: null, description: "", iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(guild.Description);
	}

	[Fact]
	public void UpdateSettings_NullDescription_KeepsExistingDescription()
	{
		var guild = CreateValid(description: "kept");

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("kept", guild.Description);
	}

	[Fact]
	public void UpdateSettings_AdvancesUpdatedAt()
	{
		var guild = CreateValid();
		var later = Now.AddHours(2);

		var result = guild.UpdateSettings(
			name: "renamed", description: null, iconUrl: null, bannerUrl: null, now: later);

		Assert.True(result.Succeeded);
		Assert.Equal(later, guild.UpdatedAt);
	}

	[Fact]
	public void UpdateSettings_RejectsEmptyName()
	{
		var guild = CreateValid();

		var result = guild.UpdateSettings(
			name: "", description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildNameRequired, result.Error);
	}

	[Fact]
	public void TransferOwnership_NonPositiveId_Fails()
	{
		var guild = CreateValid();

		var result = guild.TransferOwnership(newOwnerId: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}

	[Fact]
	public void TransferOwnership_HappyPath_UpdatesOwnerIdAndTimestamp()
	{
		var guild = CreateValid();
		var later = Now.AddMinutes(30);

		var result = guild.TransferOwnership(newOwnerId: 99, now: later);

		Assert.True(result.Succeeded);
		Assert.Equal(99L, guild.OwnerId);
		Assert.Equal(later, guild.UpdatedAt);
	}

	private static GuildEntity CreateValid(string? description = null) =>
		GuildEntity.Create(
			id: 1,
			name: "Test",
			description: description,
			iconUrl: null,
			bannerUrl: null,
			ownerId: 42,
			everyoneRoleId: 2,
			adminRoleId: 3,
			now: Now).Value;
}
