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
	public void Create_NameWithControlChar_Fails()
	{
		var result = GuildEntity.Create(
			id: 1, name: "bad\nname", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 42, everyoneRoleId: 2, adminRoleId: 3, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildNameInvalid, result.Error);
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
	public void UpdateSettings_NameAtMaxLength_Succeeds()
	{
		var guild = CreateValid();
		var name = new string('a', GuildEntity.MaxNameLen);

		var result = guild.UpdateSettings(
			name: name, description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal(name, guild.Name);
	}

	[Fact]
	public void UpdateSettings_NonEmptyDescription_SetsDescription()
	{
		var guild = CreateValid();

		var result = guild.UpdateSettings(
			name: null, description: "new description", iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("new description", guild.Description);
	}

	[Fact]
	public void UpdateSettings_EmptyIconUrl_ClearsIconUrl()
	{
		var guild = CreateValid(iconUrl: "https://example.com/icon.png");

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: "", bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(guild.IconUrl);
	}

	[Fact]
	public void UpdateSettings_NullIconUrl_KeepsExistingIconUrl()
	{
		var guild = CreateValid(iconUrl: "https://example.com/icon.png");

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("https://example.com/icon.png", guild.IconUrl);
	}

	[Fact]
	public void UpdateSettings_NonEmptyIconUrl_SetsIconUrl()
	{
		var guild = CreateValid();

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: "https://new.com/icon.png", bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("https://new.com/icon.png", guild.IconUrl);
	}

	[Fact]
	public void UpdateSettings_IconUrlAtMaxLength_Succeeds()
	{
		var guild = CreateValid();
		var url = new string('a', GuildEntity.MaxUrlLen);

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: url, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void UpdateSettings_IconUrlTooLong_Fails()
	{
		var guild = CreateValid();
		var url = new string('a', GuildEntity.MaxUrlLen + 1);

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: url, bannerUrl: null, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildIconUrlTooLong, result.Error);
	}

	[Fact]
	public void UpdateSettings_EmptyBannerUrl_ClearsBannerUrl()
	{
		var guild = CreateValid(bannerUrl: "https://example.com/banner.png");

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: "", now: Now);

		Assert.True(result.Succeeded);
		Assert.Null(guild.BannerUrl);
	}

	[Fact]
	public void UpdateSettings_NullBannerUrl_KeepsExistingBannerUrl()
	{
		var guild = CreateValid(bannerUrl: "https://example.com/banner.png");

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: null, now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("https://example.com/banner.png", guild.BannerUrl);
	}

	[Fact]
	public void UpdateSettings_NonEmptyBannerUrl_SetsBannerUrl()
	{
		var guild = CreateValid();

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: "https://new.com/banner.png", now: Now);

		Assert.True(result.Succeeded);
		Assert.Equal("https://new.com/banner.png", guild.BannerUrl);
	}

	[Fact]
	public void UpdateSettings_BannerUrlAtMaxLength_Succeeds()
	{
		var guild = CreateValid();
		var url = new string('a', GuildEntity.MaxUrlLen);

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: url, now: Now);

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void UpdateSettings_BannerUrlTooLong_Fails()
	{
		var guild = CreateValid();
		var url = new string('a', GuildEntity.MaxUrlLen + 1);

		var result = guild.UpdateSettings(
			name: null, description: null, iconUrl: null, bannerUrl: url, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.GuildBannerUrlTooLong, result.Error);
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

	[Fact]
	public void AddMember_HappyPath_AppendsAndAdvancesUpdatedAt()
	{
		var guild = CreateValid();
		var later = Now.AddMinutes(10);
		var before = guild.Members.Count;

		var result = guild.AddMember(userId: 99, now: later);

		Assert.True(result.Succeeded);
		Assert.Equal(before + 1, guild.Members.Count);
		Assert.Contains(guild.Members, m => m.UserId == 99);
		Assert.Equal(later, guild.UpdatedAt);
	}

	[Fact]
	public void AddMember_AlreadyMember_Fails()
	{
		var guild = CreateValid();

		var result = guild.AddMember(userId: guild.OwnerId, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.AlreadyMember, result.Error);
	}

	[Fact]
	public void AddMember_PropagatesGuildMemberCreateFailure()
	{
		var guild = CreateValid();

		var result = guild.AddMember(userId: 0, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.TargetNotAMember, result.Error);
	}

	[Fact]
	public void RemoveMember_HappyPath_RemovesMemberAndRoles()
	{
		var guild = CreateValid();
		guild.AddMember(userId: 99, now: Now);
		Guild.UnitTests.Fakes.DomainSeed.AssignRole(
			guild, userId: 99, roleId: guild.Roles.First(r => r.IsDefault).Id, now: Now);

		var later = Now.AddMinutes(5);
		var result = guild.RemoveMember(userId: 99, now: later);

		Assert.True(result.Succeeded);
		Assert.DoesNotContain(guild.Members, m => m.UserId == 99);
		Assert.DoesNotContain(guild.MemberRoles, mr => mr.UserId == 99);
		Assert.Equal(later, guild.UpdatedAt);
	}

	[Fact]
	public void RemoveMember_Owner_Fails()
	{
		var guild = CreateValid();

		var result = guild.RemoveMember(userId: guild.OwnerId, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.OwnerCannotLeave, result.Error);
	}

	[Fact]
	public void RemoveMember_NotAMember_Fails()
	{
		var guild = CreateValid();

		var result = guild.RemoveMember(userId: 9999, now: Now);

		Assert.True(result.IsFailure);
		Assert.Equal(GuildFailures.NotAMember, result.Error);
	}

	private static GuildEntity CreateValid(
		string? description = null,
		string? iconUrl = null,
		string? bannerUrl = null) =>
		GuildEntity.Create(
			id: 1,
			name: "Test",
			description: description,
			iconUrl: iconUrl,
			bannerUrl: bannerUrl,
			ownerId: 42,
			everyoneRoleId: 2,
			adminRoleId: 3,
			now: Now).Value;
}
