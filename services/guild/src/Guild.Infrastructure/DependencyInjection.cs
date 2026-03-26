using Guild.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Guild.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddOptions<BackendConfigurationOptions>()
				.BindConfiguration("BackendConfiguration")
				.ValidateDataAnnotations()
				.ValidateOnStart();

		services.AddOptions<RabbitMqOptions>()
			.BindConfiguration("RabbitMQ")
			.ValidateDataAnnotations()
			.ValidateOnStart();
		return services;
	}
}
