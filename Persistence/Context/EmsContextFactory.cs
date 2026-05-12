using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EMS.Persistence.Context
{
    public class EmsContextFactory : IDesignTimeDbContextFactory<EmsContext>
    {
        public EmsContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var environmentName =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("EMSContext");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Missing connection string 'ConnectionStrings:EMSContext'.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<EmsContext>();
            optionsBuilder.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(9, 0, 0)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure());
            return new EmsContext(optionsBuilder.Options);
        }
    }
}
