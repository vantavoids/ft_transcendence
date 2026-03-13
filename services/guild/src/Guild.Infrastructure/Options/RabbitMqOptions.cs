using System.ComponentModel.DataAnnotations;

namespace Guild.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    [Required] public required string Host { get; init; }
    [Required] public required string VirtualHost { get; init; }
    [Required] public required string Username { get; init; }
    [Required] public required string Password { get; init; }
}