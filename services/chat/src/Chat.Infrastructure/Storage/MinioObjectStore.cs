using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using Chat.Application.Abstractions;
using Chat.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure.Storage;

/// <summary>
/// <see cref="IObjectStore"/> backed by MinIO over the S3 API. The bucket is created
/// out-of-band by the infra layer, so this only reads and writes objects.
/// </summary>
internal sealed class MinioObjectStore(IAmazonS3 client, IOptions<MinioOptions> options) : IObjectStore
{
	private readonly string _bucket = options.Value.Bucket;

	public async Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken ct)
	{
		var request = new PutObjectRequest
		{
			BucketName = _bucket,
			Key = key,
			InputStream = content,
			ContentType = contentType,
			// hand S3 the known length up front so it doesn't probe the stream for it
			Headers = { ContentLength = length },
			AutoCloseStream = false,
		};

		await client.PutObjectAsync(request, ct);
	}

	public async Task<Stream?> GetAsync(string key, CancellationToken ct)
	{
		try
		{
			var response = await client.GetObjectAsync(_bucket, key, ct);
			return response.ResponseStream;
		}
		catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	public async Task DeleteAsync(string key, CancellationToken ct)
		=> await client.DeleteObjectAsync(_bucket, key, ct);

	public async IAsyncEnumerable<ObjectInfo> ListAsync([EnumeratorCancellation] CancellationToken ct)
	{
		var request = new ListObjectsV2Request { BucketName = _bucket };

		// page through the bucket; S3 caps each response at 1000 keys and hands back
		// a continuation token until the listing is exhausted
		while (true)
		{
			var response = await client.ListObjectsV2Async(request, ct);

			foreach (var obj in response.S3Objects ?? [])
				yield return new ObjectInfo(
					obj.Key,
					new DateTimeOffset(DateTime.SpecifyKind(obj.LastModified, DateTimeKind.Utc)));

			if (response.IsTruncated != true)
				yield break;

			request.ContinuationToken = response.NextContinuationToken;
		}
	}
}
