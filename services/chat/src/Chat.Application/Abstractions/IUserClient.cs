namespace Chat.Application.Abstractions;

public interface IUserClient
{
	Task<UsersRelationship?> GetUsersRelationship(long callerId, long recipientId, CancellationToken ct);

	/// <summary>
	/// accepted-friend ids of <paramref name="userId"/> (excluding blocks). used
	/// to fan presence/social/profile changes out to friends who may not share a
	/// guild with the user.
	/// </summary>
	Task<IReadOnlyList<long>> GetFriendIdsAsync(long userId, CancellationToken ct);
}

public sealed record UsersRelationship(string Status, DateTimeOffset? Since);
