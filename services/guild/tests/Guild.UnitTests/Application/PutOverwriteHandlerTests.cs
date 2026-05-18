using Guild.Application.Features.Channels.Permissions.Common;
using Guild.Application.Features.Channels.Permissions.PutOverwrite;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class PutOverwriteHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(999, 50, "role", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task InvalidTargetType_Returns400()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 50, "weird", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task NonExistentRoleTarget_ReturnsInvalidTarget()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 9999, "role", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task NonExistentMemberTarget_ReturnsInvalidTarget()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 9999, "member", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RoleTarget_Creates()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, roleId, "role",
				Allow: (long)Permission.SendMessages, Deny: 0L));

		Assert.True(result.Succeeded);
		Assert.Equal("role", result.Value.TargetType);
		Assert.Single(overwrites.Store);
	}

	[Fact]
	public async Task SecondPutSameTarget_UpdatesExisting()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 1L, 0L));
		await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 4L, 8L));

		Assert.Single(overwrites.Store);
		var only = overwrites.Store.Values.Single();
		Assert.Equal(4L, only.Allow);
		Assert.Equal(8L, only.Deny);
	}

	[Fact]
	public async Task AllowDenyOverlap_Rejected()
	{
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, roleId, "role", 1L, 1L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteAllowDenyOverlap", result.Error.Code);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<PutOverwriteCommand, Result<OverwriteResponse>> Handler,
		FakeGuildRepository Guilds,
		FakeChannelRepository Channels,
		FakeChannelPermissionOverwriteRepository Overwrites)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var overwrites = new FakeChannelPermissionOverwriteRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateCommand<PutOverwriteCommand, Result<OverwriteResponse>>(
			guilds, channels, overwrites,
			new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, channels, overwrites);
	}
}
