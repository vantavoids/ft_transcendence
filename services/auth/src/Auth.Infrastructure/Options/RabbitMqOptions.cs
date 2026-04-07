using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public required string Host { get; init; }

    [Range(1, 65535)] public int Port { get; init; } = 5672;

    [Required] public required string VirtualHost { get; init; }
    [Required] public required string Username { get; init; }
    [Required] public required string Password { get; init; }
}

/*
* JSON representation:
{
  "RabbitMq": {
    "Host": "rabbitmq",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest"
  }
}

*/