using System.Reflection;
using Guild.Application;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Guild.UnitTests.Fakes;

/// <summary>
/// application handlers are <c>internal sealed</c> by architectural convention
/// (see <c>HandlerConventionTests.AllHandlers_MustBe_InternalSealed</c>).
/// to exercise them from a separate test assembly without granting
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
		return (ICommandHandler<TCommand, TResult>)Instantiate(handlerType, ctorArgs);
	}

	internal static ICommandHandler<TCommand> CreateCommand<TCommand>(
		params object[] ctorArgs)
		where TCommand : class, ICommand
	{
		var handlerType = FindImplementer(typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand)));
		return (ICommandHandler<TCommand>)Instantiate(handlerType, ctorArgs);
	}

	internal static IQueryHandler<TQuery, TResult> CreateQuery<TQuery, TResult>(
		params object[] ctorArgs)
		where TQuery : class, IQuery<TResult>
		where TResult : class
	{
		var handlerType = FindImplementer(typeof(IQueryHandler<,>).MakeGenericType(typeof(TQuery), typeof(TResult)));
		return (IQueryHandler<TQuery, TResult>)Instantiate(handlerType, ctorArgs);
	}

	private static Type FindImplementer(Type closedInterface)
	{
		return ApplicationAssembly.GetTypes()
			.Single(t => t is { IsAbstract: false, IsInterface: false } &&
						 closedInterface.IsAssignableFrom(t));
	}

	/// <summary>
	/// instantiates a handler, matching <paramref name="explicitArgs"/> to the
	/// constructor positionally — except <c>ILogger&lt;T&gt;</c> parameters, which are
	/// auto-filled with <c>NullLogger&lt;T&gt;.Instance</c>. handlers are internal, so a
	/// test cannot name the closed logger type to supply one itself
	/// </summary>
	private static object Instantiate(Type handlerType, object[] explicitArgs)
	{
		var parameters = handlerType.GetConstructors().Single().GetParameters();
		var args = new object[parameters.Length];
		var next = 0;

		for (var i = 0; i < parameters.Length; i++)
		{
			var type = parameters[i].ParameterType;
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
			{
				// NullLogger<T>.Instance is a static field (the non-generic NullLogger
				// exposes it as a property), so reach for the field here
				var nullLogger = typeof(NullLogger<>).MakeGenericType(type.GetGenericArguments()[0]);
				args[i] = nullLogger.GetField("Instance")!.GetValue(null)!;
			}
			else if (type == typeof(IUnitOfWork))
			{
				// committing handlers take an IUnitOfWork; like ILogger it is plumbing,
				// not behaviour under test, so auto-fill it and let callers keep their
				// existing arg lists. the real commit path is covered by the functional
				// tests, which resolve the production UnitOfWork over the DbContext
				args[i] = new FakeUnitOfWork();
			}
			else
			{
				args[i] = explicitArgs[next++];
			}
		}

		return Activator.CreateInstance(handlerType, args)!;
	}
}
