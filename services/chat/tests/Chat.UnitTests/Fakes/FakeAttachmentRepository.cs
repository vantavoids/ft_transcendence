using Chat.Application.Abstractions.Persistence;
using Chat.Domain.Attachments;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// in-memory <see cref="IAttachmentRepository"/>. drafts are seeded directly;
/// "attaching" a draft (sending it with a message) moves it from the drafts map
/// into the attached set so <see cref="IsAttachedAsync"/> flips to true, mirroring
/// the real repo's batched draft delete + lookup insert
/// </summary>
public sealed class FakeAttachmentRepository : IAttachmentRepository
{
	private readonly Dictionary<long, DraftAttachment> _drafts = [];
	private readonly HashSet<long> _attached = [];
	private readonly Dictionary<long, AttachmentLocation> _locations = [];
	private readonly Dictionary<(long ChannelId, long MessageId), List<AttachmentMetadata>> _byMessage = [];

	public void Reset()
	{
		_drafts.Clear();
		_attached.Clear();
		_locations.Clear();
		_byMessage.Clear();
	}

	/// <summary>seed an uploaded-but-unattached draft for the resolve path</summary>
	public void SeedDraft(DraftAttachment draft) => _drafts[draft.Id] = draft;

	/// <summary>mark an attachment as already bound to a message</summary>
	public void MarkAttached(long id) => _attached.Add(id);

	public Task AddDraftAsync(DraftAttachment draft, CancellationToken ct)
	{
		_drafts[draft.Id] = draft;
		return Task.CompletedTask;
	}

	public Task<DraftAttachment?> GetDraftAsync(long id, CancellationToken ct)
		=> Task.FromResult(_drafts.GetValueOrDefault(id));

	public Task<bool> IsAttachedAsync(long id, CancellationToken ct)
		=> Task.FromResult(_attached.Contains(id));

	public Task<AttachmentLocation?> GetLocationAsync(long id, CancellationToken ct)
		=> Task.FromResult(_locations.GetValueOrDefault(id));

	public Task<AttachmentMetadata?> GetChannelAttachmentAsync(long channelId, long messageId, long id, CancellationToken ct)
	{
		var match = _byMessage.GetValueOrDefault((channelId, messageId))?
			.FirstOrDefault(a => a.Id == id);
		return Task.FromResult(match);
	}

	public Task<IReadOnlyList<AttachmentMetadata>> GetChannelMessageAttachmentsAsync(long channelId, long messageId, CancellationToken ct)
	{
		IReadOnlyList<AttachmentMetadata> result =
			_byMessage.GetValueOrDefault((channelId, messageId)) ?? [];
		return Task.FromResult(result);
	}
}
