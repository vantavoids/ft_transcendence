using Guild.Application.Contracts;
using Guild.Application.Features.Membership.LeaveGuild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class LeaveGuildHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		var handler = HandlerFactory.CreateCommand<LeaveGuildCommand, Result>(
			repo, bus, new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new LeaveGuildCommand(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task OwnerCannotLeave()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		var guild = Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<LeaveGuildCommand, Result>(
			repo, bus, new FakeClock(), new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new LeaveGuildCommand(GuildId: guild.Id));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OwnerCannotLeave", result.Error.Code);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task NotAMember_ReturnsNotAMember()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		var guild = Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<LeaveGuildCommand, Result>(
			repo, bus, new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new LeaveGuildCommand(GuildId: guild.Id));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task HappyPath_RemovesMemberAndPublishesEvent()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		var guild = Seed(repo, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 99, joinedAt: Now);
		var handler = HandlerFactory.CreateCommand<LeaveGuildCommand, Result>(
			repo, bus, new FakeClock(), new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new LeaveGuildCommand(GuildId: guild.Id));

		Assert.True(result.Succeeded);
		Assert.DoesNotContain(guild.Members, m => m.UserId == 99);

		var evt = bus.Single<GuildMemberLeft>();
		Assert.Equal(guild.Id, evt.GuildId);
		Assert.Equal(99L, evt.UserId);
	}

	private static GuildEntity Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
		return guild;
	}
}
