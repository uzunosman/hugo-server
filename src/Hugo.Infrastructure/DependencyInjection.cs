using Hugo.Infrastructure.Cache;
using Microsoft.Extensions.DependencyInjection;

namespace Hugo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string redisConnectionString)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "HugoGame:";
        });

        services.AddScoped<IGameCache, RedisGameCache>();

        return services;
    }
} 