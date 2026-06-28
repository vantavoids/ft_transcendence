using Guild.Application.Features.Membership.Common;
using Guild.Application.Features.Membership.UpdateNickname;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UpdateNicknameHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns_GuildNotFound()
	{
		var (handler, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(999, TargetUserId: 1, Nickname: "Vanta"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotMember_Returns_NotAMember()
	{
		var (handler, _) = MakeHandler(currentUser: 99);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 99, Nickname: "x"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task Self_NoExtraPerm_Succeeds()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: "spuffie"));

		Assert.True(result.Succeeded);
		Assert.Equal("spuffie", result.Value.Nickname);
		Assert.Equal("2", result.Value.UserId);
	}

	[Fact]
	public async Task Self_NullClears()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var first = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: "tmp"));
		Assert.True(first.Succeeded);

		var cleared = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: null));

		Assert.True(cleared.Succeeded);
		Assert.Null(cleared.Value.Nickname);
	}

	[Fact]
	public async Task Self_NicknameTooLong_Returns_NicknameTooLong()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: new string('a', 65)));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NicknameTooLong", result.Error.Code);
	}

	[Fact]
	public async Task Self_NicknameWithControlChars_Returns_NicknameInvalid()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: "bad\nname"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NicknameInvalid", result.Error.Code);
	}

	[Fact]
	public async Task Self_EmptyString_Returns_NicknameInvalid()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 2, Nickname: ""));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NicknameInvalid", result.Error.Code);
	}

	[Fact]
	public async Task TargetNotAMember_Returns_TargetNotAMember()
	{
		var (handler, guilds) = MakeHandler(currentUser: 1);
		// owner has ManageNicknames implicitly, target isn't a member
		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 555, Nickname: "x"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.TargetNotAMember", result.Error.Code);
	}

	[Fact]
	public async Task Other_WithoutManageNicknames_Returns_MissingPermission()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);
		DomainSeed.AddMember(guilds.Store[100], userId: 3, joinedAt: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 3, Nickname: "rude"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task Other_WithManageNicknames_AndOutRanks_Succeeds()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.ManageNicknames, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now); // bottom rank

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 3, Nickname: "renamed"));

		Assert.True(result.Succeeded);
		Assert.Equal("renamed", result.Value.Nickname);
	}

	[Fact]
	public async Task Other_WithManageNicknames_SameRank_Returns_RoleHierarchyBlocked()
	{
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var mod = DomainSeed.AddCustomRole(guild, roleId: 500, name: "Mod",
			permissions: (long)Permission.ManageNicknames, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: mod.Id, now: Now);
		DomainSeed.AddMember(guild, userId: 3, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 3, roleId: mod.Id, now: Now); // same rank

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 3, Nickname: "x"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	[Fact]
	public async Task Other_OwnerAsTarget_Returns_RoleHierarchyBlocked()
	{
		// admin-level second member tries to rename the owner; owner is unreachable
		var (handler, guilds) = MakeHandler(currentUser: 2);
		var guild = guilds.Store[100];
		var admin = DomainSeed.AddCustomRole(guild, roleId: 500, name: "FauxAdmin",
			permissions: (long)Permission.Administrator, position: 5, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: admin.Id, now: Now);

		var result = await handler.HandleAsync(new UpdateNicknameCommand(100, TargetUserId: 1, Nickname: "puppet"));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.RoleHierarchyBlocked", result.Error.Code);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<UpdateNicknameCommand, Result<MemberResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);

		var handler = HandlerFactory.CreateCommand<UpdateNicknameCommand, Result<MemberResponse>>(
			guilds, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds);
	}
}
