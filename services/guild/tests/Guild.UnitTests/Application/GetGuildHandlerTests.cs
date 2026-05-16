using Guild.Application.Features.Guilds.Common;
using Guild.Application.Features.Guilds.GetGuild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetGuildHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuildId_ReturnsGuildNotFound()
	{
		var repo = new FakeGuildRepository();
		var handler = HandlerFactory.CreateQuery<GetGuildQuery, Result<GuildDetailsDto>>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new GetGuildQuery(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task CallerNotAMember_ReturnsNotAMember()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateQuery<GetGuildQuery, Result<GuildDetailsDto>>(
			repo, new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(new GetGuildQuery(GuildId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task CallerIsAMember_ReturnsDetailsDto()
	{
		var repo = new FakeGuildRepository();
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateQuery<GetGuildQuery, Result<GuildDetailsDto>>(
			repo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new GetGuildQuery(GuildId: 100));

		Assert.True(result.Succeeded);
		var dto = result.Value;
		Assert.Equal("100", dto.Id);
		Assert.Equal("1", dto.OwnerId);
		Assert.Equal(1, dto.MemberCount);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100,
			name: "Test",
			description: null,
			iconUrl: null,
			bannerUrl: null,
			ownerId: ownerId,
			everyoneRoleId: 101,
			adminRoleId: 102,
			now: Now).Value;
		repo.AddAsync(guild).GetAwaiter().GetResult();
	}
}
