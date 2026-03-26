using Auth.Application.Abstractions.Messaging;
using Auth.Domain.Results;

namespace Auth.Application.Features.HelloWorld;

public sealed record HelloWorldQuery : IQuery<Result<string>>;
