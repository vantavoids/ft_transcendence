namespace Chat.Application.Abstractions;

/// <summary>
/// blob storage for attachment payloads (backed by S3-compatible object storage)
/// the Chat Service is the only reader/writer; clients never touch it directly so
/// authorization stays in the REST layer
/// </summary>
public interface IObjectStore
{
	Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken ct);

	/// <summary>opens a read stream for the blob, or <c>null</c> if the key is absent</summary>
	Task<Stream?> GetAsync(string key, CancellationToken ct);

	/// <summary>deletes the blob; a no-op if the key is already absent</summary>
	Task DeleteAsync(string key, CancellationToken ct);

	/// <summary>streams every stored blob's key and last-modified time, for the reaper</summary>
	IAsyncEnumerable<ObjectInfo> ListAsync(CancellationToken ct);
}

/// <summary>minimal listing entry: the object key and when it was last written</summary>
public sealed record ObjectInfo(string Key, DateTimeOffset LastModified);
