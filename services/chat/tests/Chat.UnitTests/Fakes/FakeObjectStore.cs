using System.Runtime.CompilerServices;
using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// in-memory <see cref="IObjectStore"/> standing in for MinIO. blobs are buffered
/// into byte arrays on put and replayed as fresh streams on get; each carries a
/// last-modified timestamp so the reaper's age filter can be exercised
/// </summary>
public sealed class FakeObjectStore : IObjectStore
{
	private sealed record Entry(byte[] Content, DateTimeOffset LastModified);

	private readonly Dictionary<string, Entry> _objects = [];

	public IReadOnlyDictionary<string, byte[]> Objects =>
		_objects.ToDictionary(e => e.Key, e => e.Value.Content);

	public void Reset() => _objects.Clear();

	/// <summary>seed a stored blob, last-modified now (or at an explicit time)</summary>
	public void Seed(string key, byte[] content, DateTimeOffset? lastModified = null)
		=> _objects[key] = new Entry(content, lastModified ?? DateTimeOffset.UtcNow);

	public async Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken ct)
	{
		using var buffer = new MemoryStream();
		await content.CopyToAsync(buffer, ct);
		_objects[key] = new Entry(buffer.ToArray(), DateTimeOffset.UtcNow);
	}

	public Task<Stream?> GetAsync(string key, CancellationToken ct)
		=> Task.FromResult(_objects.TryGetValue(key, out var entry)
			? new MemoryStream(entry.Content)
			: (Stream?)null);

	public Task DeleteAsync(string key, CancellationToken ct)
	{
		_objects.Remove(key);
		return Task.CompletedTask;
	}

	public async IAsyncEnumerable<ObjectInfo> ListAsync([EnumeratorCancellation] CancellationToken ct)
	{
		// snapshot so a caller may delete blobs while enumerating (the reaper does)
		foreach (var entry in _objects.Select(e => new ObjectInfo(e.Key, e.Value.LastModified)).ToList())
			yield return entry;

		await Task.CompletedTask;
	}
}
