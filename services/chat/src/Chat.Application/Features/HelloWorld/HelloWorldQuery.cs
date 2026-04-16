using Chat.Application.Abstractions.Messaging;
using Chat.Domain.Results;

namespace Chat.Application.Features.HelloWorld;

public sealed record HelloWorldQuery : IQuery<Result<string>>;
