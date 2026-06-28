using Guild.Application.Features.Guilds.CountOwnedGuilds;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class CountOwnedGuildsHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownUser_Returns_ZeroCount()
	{
		var (handler, _) = MakeHandler();

		var result = await handler.HandleAsync(new CountOwnedGuildsQuery(UserId: 999));

		Assert.True(result.Succeeded);
		Assert.Equal(0, result.Value.Count);
	}

	[Fact]
	public async Task UserOwnsZeroGuilds_Returns_ZeroCount()
	{
		var (handler, guilds) = MakeHandler();
		Seed(guilds, guildId: 100, ownerId: 1);

		var result = await handler.HandleAsync(new CountOwnedGuildsQuery(UserId: 2));

		Assert.True(result.Succeeded);
		Assert.Equal(0, result.Value.Count);
	}

	[Fact]
	public async Task UserOwnsOneGuild_Returns_One()
	{
		var (handler, guilds) = MakeHandler();
		Seed(guilds, guildId: 100, ownerId: 1);
		Seed(guilds, guildId: 200, ownerId: 2);

		var result = await handler.HandleAsync(new CountOwnedGuildsQuery(UserId: 1));

		Assert.True(result.Succeeded);
		Assert.Equal(1, result.Value.Count);
	}

	[Fact]
	public async Task UserOwnsMultipleGuilds_Returns_AccurateCount()
	{
		var (handler, guilds) = MakeHandler();
		Seed(guilds, guildId: 100, ownerId: 1);
		Seed(guilds, guildId: 200, ownerId: 1);
		Seed(guilds, guildId: 300, ownerId: 1);
		Seed(guilds, guildId: 400, ownerId: 2);

		var result = await handler.HandleAsync(new CountOwnedGuildsQuery(UserId: 1));

		Assert.True(result.Succeeded);
		Assert.Equal(3, result.Value.Count);
	}

	private static void Seed(FakeGuildRepository guilds, long guildId, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: guildId, name: $"g{guildId}", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: guildId + 1, adminRoleId: guildId + 2, now: Now).Value;
		guilds.Add(guild);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<CountOwnedGuildsQuery, Result<OwnedGuildsCountResponse>> Handler,
		FakeGuildRepository Guilds)
		MakeHandler()
	{
		var guilds = new FakeGuildRepository();
		var handler = HandlerFactory.CreateQuery<CountOwnedGuildsQuery, Result<OwnedGuildsCountResponse>>(guilds);
		return (handler, guilds);
	}
}
