using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using NLog;
using NLog.Web;
using NLog.Web.AspNetCore;
using NLog.Extensions.Logging;
using STak.TakHub.Infrastructure.Data;
using STak.TakHub.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace STak.TakHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            InitializeLogging(args);
            var host = CreateHostBuilder(args).Build();
            CreateDatabasesIfNecessary(host);
            host.Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            string applicationDirectory = GetApplicationDirectory();

            var builder = Host.CreateDefaultBuilder(args)
                .UseContentRoot(applicationDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseNLog();

            // Configure the DI container to be used (and passed to Startup.ConfigureContainer).

            var config = new ConfigurationBuilder()
                .SetBasePath(applicationDirectory)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var framework = config.GetSection("AspNetFramework");
            var container = framework[nameof(AspNetFrameworkOptions.DIContainer)];

            if (container?.ToLower() == "autofac")
            {
                builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            }

            return builder;
        }


        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }


        public static string GetDatabaseDirectory()
        {
            return Path.Combine(GetApplicationDirectory(), "Database");
        }


        private static void InitializeLogging(string[] cmdLineArgs)
        {
            string prefix = "--Environment=";
            string env = cmdLineArgs.Where(s => Regex.IsMatch(s, $"^{prefix}", RegexOptions.IgnoreCase))
                                                                                          .FirstOrDefault();
            if (env != null)
            {
                env = env[prefix.Length..];
            }
            else
            {
                env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                   ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            }

            var config = new ConfigurationBuilder()
              .SetBasePath(GetApplicationDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
              .Build();

            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("nlog"));
            NLog.Web.NLogBuilder.ConfigureNLog(LogManager.Configuration);
        }


        private static void CreateDatabasesIfNecessary(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the App dataabase.");
                }
            }

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<AppIdentityDbContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the AppIdentity dataabase.");
                }
            }
        }
    }
}
