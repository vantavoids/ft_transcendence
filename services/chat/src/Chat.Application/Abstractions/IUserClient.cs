namespace Chat.Application.Abstractions;

public interface IUserClient
{
	Task<UsersRelationship?> GetUsersRelationship(long callerId, long recipientId, CancellationToken ct);
}

public sealed record UsersRelationship(string Status, DateTimeOffset Since);
