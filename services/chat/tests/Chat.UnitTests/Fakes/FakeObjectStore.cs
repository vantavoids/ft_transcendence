using Chat.Application.Abstractions;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// in-memory <see cref="IObjectStore"/> standing in for MinIO. blobs are buffered
/// into byte arrays on put and replayed as fresh streams on get
/// </summary>
public sealed class FakeObjectStore : IObjectStore
{
	private readonly Dictionary<string, byte[]> _objects = [];

	public IReadOnlyDictionary<string, byte[]> Objects => _objects;

	public void Reset() => _objects.Clear();

	/// <summary>seed a stored blob directly (e.g. for the download path)</summary>
	public void Seed(string key, byte[] content) => _objects[key] = content;

	public async Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken ct)
	{
		using var buffer = new MemoryStream();
		await content.CopyToAsync(buffer, ct);
		_objects[key] = buffer.ToArray();
	}

	public Task<Stream?> GetAsync(string key, CancellationToken ct)
		=> Task.FromResult(_objects.TryGetValue(key, out var bytes)
			? new MemoryStream(bytes)
			: (Stream?)null);
}
