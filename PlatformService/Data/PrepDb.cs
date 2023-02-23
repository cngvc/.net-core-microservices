using Microsoft.EntityFrameworkCore;
using PlatformService.Models;

namespace PlatformService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder builder, bool isProd)
    {
        using (var serviceScope = builder.ApplicationServices.CreateScope())
        {
            SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>(), isProd);
        }
    }

    private static void SeedData(AppDbContext context, bool isProd)
    {
        if (isProd)
        {
            Console.WriteLine("--> Apply migrations");
            try
            {
                context.Database.Migrate();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"--> Could not run migrations: {ex.Message}");
            }
        }

        if (!context.Platforms.Any())
        {
            Console.WriteLine("--> Seeding data");

            context.Platforms.AddRange(
                new Platform()
                {
                    Name = "Dot Net",
                    Publisher = "Microsoft",
                    Cost = "Free"
                }, new Platform()
                {
                    Name = "SQL Server",
                    Publisher = "Microsoft",
                    Cost = "Free"
                }, new Platform()
                {
                    Name = "Kubernetes",
                    Publisher = "Cloud Native Computing Foundation",
                    Cost = "Free"
                }
            );

            context.SaveChanges();
        }
        else
        {
            Console.WriteLine("--> We already have data");
        }
    }
}