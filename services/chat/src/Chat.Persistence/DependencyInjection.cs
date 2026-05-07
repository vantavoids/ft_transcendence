using Cassandra;
using Chat.Persistence.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chat.Persistence;

public static class DependencyInjection
{
	public static IServiceCollection AddPersistence(this IServiceCollection services)
	{
		services.AddOptions<ScyllaDbOptions>()
			.BindConfiguration("ScyllaDb")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddSingleton<ISession>(sp =>
				{
					var opts = sp.GetRequiredService<IOptions<ScyllaDbOptions>>().Value;
					var builder = Cluster.Builder()
					.AddContactPoints(opts.ContactPoints.Split(','))
					.WithPort(opts.Port);

					if (!string.IsNullOrEmpty(opts.Username))
						builder.WithCredentials(opts.Username, opts.Password);
					return builder.Build().Connect(opts.Keyspace);
				});
		return services;
	}
}
