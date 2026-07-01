using Microsoft.Extensions.DependencyInjection;
using StockFlow.Core.Configuration;
using StockFlow.Core.Data;
using StockFlow.Core.Services;

namespace StockFlow.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStockFlowCore(this IServiceCollection services, string connectionString)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.Configure<DatabaseOptions>(o => o.ConnectionString = connectionString);
        services.AddSingleton<DbConnectionFactory>();
        services.AddScoped<ProductService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<StockService>();

        return services;
    }
}
