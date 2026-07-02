using Cassandra;
using Chat.Application.Abstractions.Persistence;
using Chat.Persistence.Health;
using Chat.Persistence.Options;
using Chat.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

		services.AddSingleton<MessageStatements>();
		services.AddScoped<IMessageRepository, MessageRepository>();
		services.AddSingleton<AttachmentStatements>();
		services.AddScoped<IAttachmentRepository, AttachmentRepository>();
		return services;
	}

	public static IHealthChecksBuilder AddPersistenceHealthChecks(this IHealthChecksBuilder builder)
		=> builder.AddCheck<ScyllaHealthCheck>("scylla");
}
