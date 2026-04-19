namespace Auth.Application.Abstractions.Security;

public interface ISecretHasher
{
    string Hash(string value);
    bool Verify(string value, string hash);
}