using Chat.Application.Abstractions.Persistence;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// in-memory stand-in for the Scylla-backed export repo. tests seed the two lists
/// and the endpoint/handler/serialization are exercised against them; the real
/// Cassandra query path is verified at runtime against a live Scylla.
/// </summary>
public sealed class FakeDataExportRepository : IDataExportRepository
{
	public List<ExportedChannelMessage> ChannelMessages { get; } = [];
	public List<ExportedDirectMessage> DirectMessages { get; } = [];

	public Task<ChatUserDataExport> GetUserDataExportAsync(long userId, CancellationToken ct) =>
		Task.FromResult(new ChatUserDataExport(ChannelMessages, DirectMessages));
}
