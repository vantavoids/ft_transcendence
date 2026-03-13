using Guild.Application.Abstractions.Messaging;
using Guild.Domain.Results;

namespace Guild.Application.Features.HelloWorld;

public sealed record HelloWorldQuery : IQuery<Result<string>>;