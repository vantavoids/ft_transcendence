using Chat.Application.Abstractions;
using Chat.Infrastructure.Messaging;
using Chat.Infrastructure.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddOptions<RabbitMqOptions>()
			.BindConfiguration("RabbitMQ")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddMassTransit(x =>
		{
			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
				});
				cfg.ConfigureEndpoints(ctx);
			});
		});

		services.AddScoped<IEventBus, EventBus>();

		return services;
	}
}
