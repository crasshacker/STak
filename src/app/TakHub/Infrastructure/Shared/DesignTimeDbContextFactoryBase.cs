using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace STak.TakHub.Infrastructure.Shared
{
    public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
                                                                               where TContext : DbContext
    {
        private const string DefaultDatabaseProvider = "sqlite";

        protected abstract TContext CreateNewInstance(DbContextOptions<TContext> options);


        public TContext CreateDbContext(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var providerName    = Environment.GetEnvironmentVariable("ASPNETCORE_DATABASE_PROVIDER");

            return Create(Directory.GetCurrentDirectory(), environmentName, providerName);
        }


        public TContext Create()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var providerName    = Environment.GetEnvironmentVariable("ASPNETCORE_DATABASE_PROVIDER");
            var basePath        = AppContext.BaseDirectory;

            return Create(basePath, environmentName, providerName);
        }


        private TContext Create(string basePath, string environmentName, string providerName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environmentName}.json", true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            if (string.IsNullOrWhiteSpace(providerName))
            {
                providerName = DefaultDatabaseProvider;
            }

            // TODO - Use different keys for Sqlite and SqlServer connection strings.

            var connectionString = config.GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Could not find a connection string named 'Default'.");
            }

            return Create(providerName, connectionString);
        }


        private TContext Create(string providerName, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException($"{nameof(connectionString)} is null or empty.", nameof(connectionString));
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentException($"{nameof(providerName)} is null or empty.", nameof(providerName));

            var optionsBuilder = new DbContextOptionsBuilder<TContext>();

            Console.WriteLine("DesignTimeDbContextFactory.Create(string): Database Provider: {0}", providerName);
            Console.WriteLine("DesignTimeDbContextFactory.Create(string): Connection string: {0}", connectionString);

            if (providerName.ToLower() == "sqlite")
            {
                optionsBuilder.UseSqlite(connectionString);
            }
            if (providerName.ToLower() == "sqlserver")
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            var options = optionsBuilder.Options;
            return CreateNewInstance(options);
        }
    }
}
