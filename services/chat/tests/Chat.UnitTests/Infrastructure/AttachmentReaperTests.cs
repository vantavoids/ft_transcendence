using Chat.Application.Abstractions.Persistence;
using Chat.Infrastructure.Options;
using Chat.Infrastructure.Storage;
using Chat.UnitTests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chat.UnitTests.Infrastructure;

public sealed class AttachmentReaperTests
{
	private const int MinAgeSeconds = 7200; // 2h, matches the default

	private static (AttachmentReaper Reaper, FakeObjectStore Store, FakeAttachmentRepository Attachments) Build()
	{
		var store = new FakeObjectStore();
		var attachments = new FakeAttachmentRepository();

		// IAttachmentRepository is resolved per-sweep through a scope, so back the
		// scope factory with a real container holding the fake
		var services = new ServiceCollection();
		services.AddSingleton<IAttachmentRepository>(attachments);
		var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

		var options = Options.Create(new AttachmentReaperOptions
		{
			Enabled = true,
			IntervalSeconds = 900,
			MinAgeSeconds = MinAgeSeconds,
		});

		var reaper = new AttachmentReaper(scopeFactory, store, options, NullLogger<AttachmentReaper>.Instance);
		return (reaper, store, attachments);
	}

	private static DateTimeOffset Old => DateTimeOffset.UtcNow.AddSeconds(-(MinAgeSeconds + 600));
	private static DateTimeOffset Fresh => DateTimeOffset.UtcNow;

	[Fact]
	public async Task DeletesOldUnattachedBlob()
	{
		var (reaper, store, _) = Build();
		store.Seed("100", [1, 2, 3], Old);

		var reaped = await reaper.ReapOnceAsync(CancellationToken.None);

		Assert.Equal(1, reaped);
		Assert.False(store.Objects.ContainsKey("100"));
	}

	[Fact]
	public async Task KeepsOldAttachedBlob()
	{
		var (reaper, store, attachments) = Build();
		store.Seed("200", [1, 2, 3], Old);
		attachments.MarkAttached(200);

		var reaped = await reaper.ReapOnceAsync(CancellationToken.None);

		Assert.Equal(0, reaped);
		Assert.True(store.Objects.ContainsKey("200"));
	}

	[Fact]
	public async Task KeepsFreshUnattachedBlob_StillWithinDraftLifetime()
	{
		var (reaper, store, _) = Build();
		store.Seed("300", [1, 2, 3], Fresh);

		var reaped = await reaper.ReapOnceAsync(CancellationToken.None);

		Assert.Equal(0, reaped);
		Assert.True(store.Objects.ContainsKey("300"));
	}

	[Fact]
	public async Task IgnoresBlobsWhoseKeyIsNotAnAttachmentId()
	{
		var (reaper, store, _) = Build();
		store.Seed("not-a-snowflake", [1, 2, 3], Old);

		var reaped = await reaper.ReapOnceAsync(CancellationToken.None);

		Assert.Equal(0, reaped);
		Assert.True(store.Objects.ContainsKey("not-a-snowflake"));
	}

	[Fact]
	public async Task SweepsMixedBucket_DeletingOnlyOrphans()
	{
		var (reaper, store, attachments) = Build();
		store.Seed("100", [1], Old);    // orphan -> deleted
		store.Seed("200", [1], Old);    // attached -> kept
		store.Seed("300", [1], Fresh);  // too fresh -> kept
		attachments.MarkAttached(200);

		var reaped = await reaper.ReapOnceAsync(CancellationToken.None);

		Assert.Equal(1, reaped);
		Assert.False(store.Objects.ContainsKey("100"));
		Assert.True(store.Objects.ContainsKey("200"));
		Assert.True(store.Objects.ContainsKey("300"));
	}
}
