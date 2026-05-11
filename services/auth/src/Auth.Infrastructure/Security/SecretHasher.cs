using System.Security.Cryptography;
using System.Text;
using Auth.Application.Abstractions.Security;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Auth.Infrastructure.Security;

internal sealed class SecretHasher : ISecretHasher
{
    private const int WorkFactor = 12;

    public string Hash(string value) =>
        BCryptNet.EnhancedHashPassword(value, workFactor: WorkFactor);

    public bool Verify(string value, string hash) => BCryptNet.EnhancedVerify(value, hash);

    public string HashDeterministic(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
