using Auth.Application.Abstractions.Security;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Auth.Infrastructure.Security;

internal sealed class BcryptHasher : ISecretHasher
{
    private const int WorkFactor = 12;

    public string Hash(string value) =>
        BCryptNet.EnhancedHashPassword(value, workFactor: WorkFactor);

    public bool Verify(string value, string hash) => BCryptNet.EnhancedVerify(value, hash);
}
