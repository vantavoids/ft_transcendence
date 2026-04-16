using Auth.Domain.Results;

namespace Auth.Domain.ValueObjects;

public sealed record Email
{
    private Email(string value, bool isVerified)
    {
        Value = value;
        IsVerified = isVerified;
    }

    public const uint MaxEmailLen = 255;

    public string Value { get; }
    public bool   IsVerified { get; }

#nullable enable
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return AuthFailures.InvalidEmail;

        var normalized = value.Trim().ToLowerInvariant();

        if (!IsValidFormat(normalized))
            return AuthFailures.InvalidEmail;

        return new Email(normalized, false);
    }
#nullable disable

    public Email Verify() => new(Value, true);

    public override string ToString() => Value;

    private static bool IsValidFormat(string value)
    {
        if (value.Length > MaxEmailLen)
            return false;

        var atIndex = value.LastIndexOf('@');
        return atIndex >= 0 && atIndex < value.Length - 1;
    }
}