﻿using Microsoft.EntityFrameworkCore;

namespace Revalidate.Configuration;

public static class DataConfiguration
{
    public static void AddDataServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            var connectionStr = config.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));
            //.ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted)); // should be configurable
            //options.UseInMemoryDatabase("Revalidate");
        });
    }

    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
}