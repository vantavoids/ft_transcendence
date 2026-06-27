using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Options;

public sealed class AttachmentReaperOptions
{
	/// <summary>master switch; turned off in tests so it never reaps seeded blobs</summary>
	public bool Enabled { get; init; } = true;

	/// <summary>how often the orphan sweep runs</summary>
	[Range(60, int.MaxValue)]
	public int IntervalSeconds { get; init; } = 900; // 15 minutes

	/// <summary>
	/// only blobs older than this are eligible for reaping. it MUST exceed the
	/// draft_attachments TTL (3600s) so a still-sendable draft is never deleted:
	/// past the TTL the draft row is gone and the blob can no longer be attached
	/// </summary>
	[Range(3601, int.MaxValue)]
	public int MinAgeSeconds { get; init; } = 7200; // 2 hours
}
