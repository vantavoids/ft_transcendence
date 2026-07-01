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
	private readonly Dictionary<(long ConversationId, long MessageId), List<AttachmentMetadata>> _byDmMessage = [];

	public void Reset()
	{
		_drafts.Clear();
		_attached.Clear();
		_locations.Clear();
		_byMessage.Clear();
		_byDmMessage.Clear();
	}

	/// <summary>seed an uploaded-but-unattached draft for the resolve path</summary>
	public void SeedDraft(DraftAttachment draft) => _drafts[draft.Id] = draft;

	/// <summary>mark an attachment as already bound to a message</summary>
	public void MarkAttached(long id) => _attached.Add(id);

	/// <summary>
	/// seed an attachment already bound to a channel message, so the download path's
	/// lookup + per-message read both resolve (mirrors the real attachment_lookup +
	/// message_attachments rows written when a draft is sent)
	/// </summary>
	public void SeedChannelAttachment(long channelId, long messageId, AttachmentMetadata metadata)
	{
		_attached.Add(metadata.Id);
		_locations[metadata.Id] = new AttachmentLocation(
			IsDm: false, ChannelId: channelId, ConversationId: null, MessageId: messageId);

		if (!_byMessage.TryGetValue((channelId, messageId), out var list))
			_byMessage[(channelId, messageId)] = list = [];
		list.Add(metadata);
	}

	/// <summary>
	/// seed an attachment already bound to a DM message, so the download path's
	/// lookup + per-message read both resolve (mirrors the real attachment_lookup +
	/// dm_attachments rows written when a draft is sent)
	/// </summary>
	public void SeedDmAttachment(long conversationId, long messageId, AttachmentMetadata metadata)
	{
		_attached.Add(metadata.Id);
		_locations[metadata.Id] = new AttachmentLocation(
			IsDm: true, ChannelId: null, ConversationId: conversationId, MessageId: messageId);

		if (!_byDmMessage.TryGetValue((conversationId, messageId), out var list))
			_byDmMessage[(conversationId, messageId)] = list = [];
		list.Add(metadata);
	}

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

	public Task<ILookup<long, AttachmentMetadata>> GetChannelMessagesAttachmentsAsync(
		long channelId, IReadOnlyList<long> messageIds, CancellationToken ct)
	{
		var wanted = messageIds.ToHashSet();
		var lookup = _byMessage
			.Where(e => e.Key.ChannelId == channelId && wanted.Contains(e.Key.MessageId))
			.SelectMany(e => e.Value.Select(m => (e.Key.MessageId, Metadata: m)))
			.ToLookup(x => x.MessageId, x => x.Metadata);
		return Task.FromResult(lookup);
	}

	public Task<AttachmentMetadata?> GetDmAttachmentAsync(long conversationId, long messageId, long id, CancellationToken ct)
	{
		var match = _byDmMessage.GetValueOrDefault((conversationId, messageId))?
			.FirstOrDefault(a => a.Id == id);
		return Task.FromResult(match);
	}

	public Task<IReadOnlyList<AttachmentMetadata>> GetDmMessageAttachmentsAsync(long conversationId, long messageId, CancellationToken ct)
	{
		IReadOnlyList<AttachmentMetadata> result =
			_byDmMessage.GetValueOrDefault((conversationId, messageId)) ?? [];
		return Task.FromResult(result);
	}

	public Task<ILookup<long, AttachmentMetadata>> GetDmMessagesAttachmentsAsync(
		long conversationId, IReadOnlyList<long> messageIds, CancellationToken ct)
	{
		var wanted = messageIds.ToHashSet();
		var lookup = _byDmMessage
			.Where(e => e.Key.ConversationId == conversationId && wanted.Contains(e.Key.MessageId))
			.SelectMany(e => e.Value.Select(m => (e.Key.MessageId, Metadata: m)))
			.ToLookup(x => x.MessageId, x => x.Metadata);
		return Task.FromResult(lookup);
	}
}
