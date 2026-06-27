using System.Reflection;
using Auth.Application;
using Auth.Application.Abstractions.Messaging;

namespace Auth.UnitTests.Fakes;

/// <summary>
/// application handlers are <c>internal sealed</c> by convention.
/// locate the concrete type via reflection and instantiate it without needing
/// <c>InternalsVisibleTo</c> on the production assembly.
/// </summary>
internal static class HandlerFactory
{
    private static readonly Assembly ApplicationAssembly =
        typeof(IApplicationAssemblyMarker).Assembly;

    internal static ICommandHandler<TCommand, TResult> CreateCommand<TCommand, TResult>(
        params object[] ctorArgs)
        where TCommand : class, ICommand<TResult>
        where TResult : class
    {
        var handlerType = FindImplementer(
            typeof(ICommandHandler<,>).MakeGenericType(typeof(TCommand), typeof(TResult)));
        return (ICommandHandler<TCommand, TResult>)Activator.CreateInstance(handlerType, ctorArgs)!;
    }

    internal static IQueryHandler<TQuery, TResponse> CreateQuery<TQuery, TResponse>(
        params object[] ctorArgs)
        where TQuery : class, IQuery<TResponse>
        where TResponse : class
    {
        var handlerType = FindImplementer(
            typeof(IQueryHandler<,>).MakeGenericType(typeof(TQuery), typeof(TResponse)));
        return (IQueryHandler<TQuery, TResponse>)Activator.CreateInstance(handlerType, ctorArgs)!;
    }

    private static Type FindImplementer(Type closedInterface)
    {
        return ApplicationAssembly.GetTypes()
            .Single(t => t is { IsAbstract: false, IsInterface: false } &&
                        closedInterface.IsAssignableFrom(t));
    }
}
