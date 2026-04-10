namespace Chat.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
	where TQuery : class, IQuery<TResponse>
	where TResponse : class
{
	Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
