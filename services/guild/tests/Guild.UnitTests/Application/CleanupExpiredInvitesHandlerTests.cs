using Guild.Application.Abstractions.Messaging;
using Guild.Application.Features.Invites.CleanupExpiredInvites;
using Guild.Domain.Guild;
using Guild.UnitTests.Fakes;
using Xunit;

namespace Guild.UnitTests.Application;

public sealed class CleanupExpiredInvitesHandlerTests
{
	// handler grace window is 7 days. anchor "now" so the cutoff is a round date:
	// cutoff = Now - 7d = 2026-01-03. anything that expired strictly before that is
	// past grace; anything between the cutoff and Now is expired-but-within-grace
	private static readonly DateTimeOffset Now =
		new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

	private const long GuildId = 1;
	private const long Creator = 1;

	[Fact]
	public async Task DeletesRevokedAndPastGrace_KeepsEverythingElse()
	{
		var invites = new FakeGuildInviteRepository();

		var revoked = Seed(invites, "revoked", expiresAt: null, createdAt: Now.AddDays(-1));
		revoked.Revoke();
		Seed(invites, "past-grace", expiresAt: Now.AddDays(-8), createdAt: Now.AddDays(-30));   // < cutoff
		Seed(invites, "within-grace", expiresAt: Now.AddDays(-2), createdAt: Now.AddDays(-30));  // expired, but inside grace
		Seed(invites, "future", expiresAt: Now.AddDays(30), createdAt: Now.AddDays(-1));         // still active
		Seed(invites, "never", expiresAt: null, createdAt: Now.AddDays(-1));                     // never expires

		var handler = NewHandler(invites);

		await handler.HandleAsync(new CleanupExpiredInvitesCommand());

		Assert.False(invites.Store.ContainsKey("revoked"));
		Assert.False(invites.Store.ContainsKey("past-grace"));
		Assert.Equal(["future", "never", "within-grace"], invites.Store.Keys.Order());
	}

	[Fact]
	public async Task RevokedInvite_DeletedRegardlessOfExpiry()
	{
		var invites = new FakeGuildInviteRepository();
		// revoked but expiry is far in the future: revocation alone makes it eligible
		var invite = Seed(invites, "revoked-future", expiresAt: Now.AddDays(30), createdAt: Now.AddDays(-1));
		invite.Revoke();

		var handler = NewHandler(invites);

		await handler.HandleAsync(new CleanupExpiredInvitesCommand());

		Assert.Empty(invites.Store);
	}

	[Fact]
	public async Task NothingEligible_LeavesStoreUntouched()
	{
		var invites = new FakeGuildInviteRepository();
		Seed(invites, "within-grace", expiresAt: Now.AddDays(-2), createdAt: Now.AddDays(-30));
		Seed(invites, "never", expiresAt: null, createdAt: Now.AddDays(-1));

		var handler = NewHandler(invites);

		await handler.HandleAsync(new CleanupExpiredInvitesCommand());

		Assert.Equal(2, invites.Store.Count);
	}

	private static ICommandHandler<CleanupExpiredInvitesCommand> NewHandler(FakeGuildInviteRepository invites)
		=> HandlerFactory.CreateCommand<CleanupExpiredInvitesCommand>(
			invites,
			new FakeClock(Now));

	private static GuildInvite Seed(
		FakeGuildInviteRepository repo, string code, DateTimeOffset? expiresAt, DateTimeOffset createdAt)
	{
		// Create rejects an expiry at/before the creation instant, so seed each invite
		// "as of" createdAt; the handler later evaluates them against the fixed Now
		var invite = GuildInvite.Create(code, GuildId, Creator, maxUses: null, expiresAt, createdAt).Value;
		repo.Seed(invite);
		return invite;
	}
}
