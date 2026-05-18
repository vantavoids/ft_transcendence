namespace Chat.Application.Abstractions.Authentication;

public interface ICurrentUser
{
	long UserId { get; }
}
