using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    [Required] public required string Host { get; init; }
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