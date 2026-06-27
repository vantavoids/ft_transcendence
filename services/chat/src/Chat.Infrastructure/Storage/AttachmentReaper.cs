using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Persistence;
using Chat.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Storage;

/// <summary>
/// periodically deletes orphaned attachment blobs from object storage. an orphan is
/// any blob that is (a) older than a draft's whole sendable lifetime yet (b) not
/// bound to a message. this covers both a never-sent draft (its row TTL'd out) and a
/// blob whose draft-row write failed after the upload. the message table is the sole
/// source of truth via <see cref="IAttachmentRepository.IsAttachedAsync"/>, so the
/// sweep can never delete a legitimately attached blob. deletes are idempotent, so
/// running this on several replicas at once is harmless
/// </summary>
public sealed class AttachmentReaper(
	IServiceScopeFactory scopeFactory,
	IObjectStore objectStore,
	IOptions<AttachmentReaperOptions> options,
	ILogger<AttachmentReaper> logger)
	: BackgroundService
{
	private readonly AttachmentReaperOptions _options = options.Value;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Enabled)
			return;

		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
		do
		{
			try
			{
				await ReapOnceAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				// a transient storage / db hiccup must not kill the loop; the next
				// tick retries the sweep
				logger.LogError(ex, "attachment reaper sweep failed");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	/// <summary>runs a single sweep; exposed so it can be exercised deterministically</summary>
	public async Task<int> ReapOnceAsync(CancellationToken ct)
	{
		var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(_options.MinAgeSeconds);

		// IAttachmentRepository is scoped (it shares the request-scoped Scylla usage),
		// so resolve it inside a fresh scope rather than capturing it in this singleton
		using var scope = scopeFactory.CreateScope();
		var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();

		var reaped = 0;
		await foreach (var obj in objectStore.ListAsync(ct))
		{
			// younger than the cutoff: a draft could still be live and sendable
			if (obj.LastModified > cutoff)
				continue;

			// keys are attachment snowflakes; anything else isn't ours to touch
			if (!long.TryParse(obj.Key, out var id))
				continue;

			// bound to a message -> permanent, keep it
			if (await attachments.IsAttachedAsync(id, ct))
				continue;

			await objectStore.DeleteAsync(obj.Key, ct);
			reaped++;
		}

		if (reaped > 0)
			logger.LogInformation("attachment reaper deleted {Count} orphaned blob(s)", reaped);

		return reaped;
	}
}
