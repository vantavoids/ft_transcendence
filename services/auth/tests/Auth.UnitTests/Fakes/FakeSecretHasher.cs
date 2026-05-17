using Auth.Application.Abstractions.Security;

namespace Auth.UnitTests.Fakes;

internal sealed class FakeSecretHasher : ISecretHasher
{
    public string Hash(string value) => $"hashed:{value}";

    public bool Verify(string value, string hash) => hash == $"hashed:{value}";

    public string HashDeterministic(string value) => $"det:{value}";
}
