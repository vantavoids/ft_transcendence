using Guild.Domain.Guild;

namespace Guild.Application.Features.Categories.Common;

/// <summary>
/// wire shape for a ChannelCategory. snowflake IDs are converted to quoted
/// strings at the DTO boundary so JS clients can hold them without precision loss
/// </summary>
public sealed record CategoryResponse(
	string Id,
	string GuildId,
	string Name,
	int Position)
{
	public static CategoryResponse From(ChannelCategory category) => new(
		Id: category.Id.ToString(),
		GuildId: category.GuildId.ToString(),
		Name: category.Name,
		Position: category.Position);
}
