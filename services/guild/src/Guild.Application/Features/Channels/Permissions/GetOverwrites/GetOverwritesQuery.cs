using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.GetOverwrites;

public sealed record GetOverwritesQuery(long ChannelId) : IQuery<Result<OverwritesResponse>>;

/// <summary>
/// wrapped in a record so the generic <c>IQueryHandler&lt;,&gt;</c> constraint
/// (<c>TResponse : class</c>) is satisfied; serialised by Carter as a JSON
/// array via <see cref="Items"/>
/// </summary>
public sealed record OverwritesResponse(IReadOnlyList<OverwriteItem> Items);

public sealed record OverwriteItem(string TargetId, string TargetType, long Allow, long Deny)
{
	public static OverwriteItem From(ChannelPermissionOverwrite o) => new(
		TargetId: o.TargetId.ToString(),
		TargetType: o.TargetType == OverwriteTargetType.Role ? "role" : "user",
		Allow: o.Allow,
		Deny: o.Deny);
}
