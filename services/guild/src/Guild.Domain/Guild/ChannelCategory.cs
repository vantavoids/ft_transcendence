using Guild.Domain.Results;

namespace Guild.Domain.Guild;

public sealed class ChannelCategory
{
	public const int MaxNameLen = 100;

	// EF Core constructor
	private ChannelCategory() { }

	private ChannelCategory(
		long id,
		long guildId,
		string name,
		int position,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		GuildId = guildId;
		Name = name;
		Position = position;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	public long Id { get; private set; }
	public long GuildId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public int Position { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static Result<ChannelCategory> Create(
		long id,
		long guildId,
		string? name,
		int position,
		DateTimeOffset now)
	{
		var validation = ValidateName(name);
		if (validation.IsFailure)
			return validation.Error;

		return new ChannelCategory(
			id: id,
			guildId: guildId,
			name: name!,
			position: position,
			createdAt: now,
			updatedAt: now);
	}

	public Result Rename(string? name, DateTimeOffset now)
	{
		var validation = ValidateName(name);
		if (validation.IsFailure)
			return validation;

		Name = name!;
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result Reposition(int position, DateTimeOffset now)
	{
		Position = position;
		UpdatedAt = now;
		return Result.Ok();
	}

	private static Result ValidateName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return GuildFailures.CategoryNameRequired;

		if (name.Length > MaxNameLen)
			return GuildFailures.CategoryNameTooLong;

		return Result.Ok();
	}
}
