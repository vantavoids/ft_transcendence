namespace Auth.Application.Abstractions.Security;

public interface ITokenGenerator
{
    string GenerateAccessToken(long userId);
    string GenerateRefreshToken();
}