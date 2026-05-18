using Guild.Application.Features.Channels.Permissions.DeleteOverwrite;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteOverwriteHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new DeleteOverwriteCommand(999, 50));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NoMatchingOverwrite_Returns404()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new DeleteOverwriteCommand(5, 9999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesOverwrite()
	{
		var (handler, _, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			1, 5, OverwriteTargetType.Role, 50, 1L, 0L, Now).Value);

		var result = await handler.HandleAsync(new DeleteOverwriteCommand(5, 50));

		Assert.True(result.Succeeded);
		Assert.Empty(overwrites.Store);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<DeleteOverwriteCommand, Result> Handler,
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

		var handler = HandlerFactory.CreateCommand<DeleteOverwriteCommand, Result>(
			guilds, channels, overwrites, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, channels, overwrites);
	}
}
