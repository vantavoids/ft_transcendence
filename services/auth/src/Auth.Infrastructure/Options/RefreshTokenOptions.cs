using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

#nullable enable

[Serializable]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EvenNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext? validationContext)
    {
        if (value is null) return ValidationResult.Success;

        if (value is int intValue && intValue % 2 == 0)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "The value must be an even number.");
    }
}

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    [Required, Range(1, int.MaxValue)]
    public required int TtlDays { get; init; }

    [Required, Range(32, 512), EvenNumber()]
    public required int ByteLength { get; init; }

    [Required]
    public required string CookieName { get; init; }
    public bool HttpOnly { get; init; } = true;
    public bool Secure { get; init; } = true;
}