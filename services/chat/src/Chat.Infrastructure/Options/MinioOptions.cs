using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Options;

public sealed class MinioOptions
{
	[Required] public required string Endpoint { get; init; }
	[Required] public required string AccessKey { get; init; }
	[Required] public required string SecretKey { get; init; }
	[Required] public required string Bucket { get; init; }
}
