using System.Reflection;
using Chat.Application;
using Chat.Application.Abstractions.Messaging;

namespace Chat.UnitTests.Fakes;

/// <summary>
/// application handlers are <c>internal sealed</c> by architectural convention
/// (see <c>HandlerConventionTests.AllHandlers_MustBe_InternalSealed</c>). to
/// exercise them from a separate test assembly without granting
/// <c>InternalsVisibleTo</c>, locate the concrete handler in the Application
/// assembly by the interface it implements and instantiate it via reflection
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
		var handlerType = FindImplementer(typeof(ICommandHandler<,>).MakeGenericType(typeof(TCommand), typeof(TResult)));
		return (ICommandHandler<TCommand, TResult>)Activator.CreateInstance(handlerType, ctorArgs)!;
	}

	internal static IQueryHandler<TQuery, TResult> CreateQuery<TQuery, TResult>(
		params object[] ctorArgs)
		where TQuery : class, IQuery<TResult>
		where TResult : class
	{
		var handlerType = FindImplementer(typeof(IQueryHandler<,>).MakeGenericType(typeof(TQuery), typeof(TResult)));
		return (IQueryHandler<TQuery, TResult>)Activator.CreateInstance(handlerType, ctorArgs)!;
	}

	private static Type FindImplementer(Type closedInterface)
	{
		return ApplicationAssembly.GetTypes()
			.Single(t => t is { IsAbstract: false, IsInterface: false } &&
						 closedInterface.IsAssignableFrom(t));
	}
}
