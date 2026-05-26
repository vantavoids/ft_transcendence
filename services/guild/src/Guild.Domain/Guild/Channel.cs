using Guild.Domain.Results;

namespace Guild.Domain.Guild;

/// <summary>
/// a chat or voice channel inside a guild. lives under an optional category.
/// per-channel permission tweaks are represented by <see cref="ChannelPermissionOverwrite"/>
/// </summary>
public sealed class Channel
{
	public const int MaxNameLen = 100;
	public const int MaxTopicLen = 1024;

	// EF Core constructor
	private Channel() { }

	private Channel(
		long id,
		long guildId,
		long? categoryId,
		string name,
		string? topic,
		ChannelType type,
		int position,
		bool isNsfw,
		int slowmodeSeconds,
		DateTimeOffset createdAt,
		DateTimeOffset updatedAt)
	{
		Id = id;
		GuildId = guildId;
		CategoryId = categoryId;
		Name = name;
		Topic = topic;
		Type = type;
		Position = position;
		IsNsfw = isNsfw;
		SlowmodeSeconds = slowmodeSeconds;
		CreatedAt = createdAt;
		UpdatedAt = updatedAt;
	}

	public long Id { get; private set; }
	public long GuildId { get; private set; }
	public long? CategoryId { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string? Topic { get; private set; }
	public ChannelType Type { get; private set; }
	public int Position { get; private set; }
	public bool IsNsfw { get; private set; }
	public int SlowmodeSeconds { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public DateTimeOffset UpdatedAt { get; private set; }

	public static Result<Channel> Create(
		long id,
		long guildId,
		long? categoryId,
		string? name,
		string? topic,
		ChannelType type,
		int position,
		DateTimeOffset now,
		bool isNsfw = false,
		int slowmodeSeconds = 0)
	{
		var nameValidation = ValidateName(name);
		if (nameValidation.IsFailure)
			return nameValidation.Error;

		var topicValidation = ValidateTopic(topic);
		if (topicValidation.IsFailure)
			return topicValidation.Error;

		if (!Enum.IsDefined(type))
			return GuildFailures.ChannelInvalidType;

		if (position < 0)
			position = 0;

		if (slowmodeSeconds < 0)
			slowmodeSeconds = 0;

		return new Channel(
			id: id,
			guildId: guildId,
			categoryId: categoryId,
			name: name!,
			topic: topic,
			type: type,
			position: position,
			isNsfw: isNsfw,
			slowmodeSeconds: slowmodeSeconds,
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

	public Result UpdateTopic(string? topic, DateTimeOffset now)
	{
		var validation = ValidateTopic(topic);
		if (validation.IsFailure)
			return validation;

		// empty string clears the topic
		Topic = string.IsNullOrEmpty(topic) ? null : topic;
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result Reposition(int position, DateTimeOffset now)
	{
		if (position < 0)
			position = 0;

		Position = position;
		UpdatedAt = now;
		return Result.Ok();
	}

	public Result MoveToCategory(long? categoryId, DateTimeOffset now)
	{
		CategoryId = categoryId;
		UpdatedAt = now;
		return Result.Ok();
	}

	private static Result ValidateName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return GuildFailures.ChannelNameRequired;

		if (name.Length > MaxNameLen)
			return GuildFailures.ChannelNameTooLong;

		if (!name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-' || c == '_'))
			return GuildFailures.ChannelNameInvalid;

		return Result.Ok();
	}

	private static Result ValidateTopic(string? topic)
	{
		if (topic is not null && topic.Length > MaxTopicLen)
			return GuildFailures.ChannelTopicTooLong;

		return Result.Ok();
	}
}
