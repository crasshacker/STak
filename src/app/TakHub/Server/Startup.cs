#define NEWTONSOFT_JSON_SERIALIZATION
#define SYSTEMTEXT_JSON_SERIALIZATION
#define MESSAGEPACK_SERIALIZATION

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using NLog;
using Swashbuckle.AspNetCore.Swagger;
using FluentValidation.AspNetCore;
#if NEWTONSOFT_JSON_SERIALIZATION
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif
using STak.TakHub.Core;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Hubs;
using STak.TakHub.Interop;
using STak.TakHub.Presenters;
using STak.TakHub.Helpers;
using STak.TakHub.Extensions;
using STak.TakHub.Infrastructure;
using STak.TakHub.Infrastructure.Auth;
using STak.TakHub.Infrastructure.Data;
using STak.TakHub.Infrastructure.Helpers;
using STak.TakHub.Infrastructure.Identity;
using STak.TakHub.Models.Settings;
using STak.TakEngine.Actors;
using STak.TakEngine.AI;
using Microsoft.Extensions.Hosting;

namespace STak.TakHub
{
    public class SignalROptions
    {
        public string   Protocol      { get; set; }
        public TimeSpan ClientTimeout { get; set; }
    }


    public class AspNetFrameworkOptions
    {
        public string         DIContainer { get; set; }
        public SignalROptions SignalR     { get; set; }
    }


    public class Startup
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            InitializeEngine();
        }


        public void ConfigureServices(IServiceCollection services)
        {
            s_logger.Debug("Configuring services...");

            ConfigureTakHubServices(services);
            ConfigureDatabaseServices(services);
            ConfigureAuthenticationServices(services);
            ConfigureMvcServices(services);
            ConfigureSignalRServices(services);
            ConfigureIdentityServices(services);
            ConfigureSwaggerServices(services);
            ConfigureTypeMappingServices(services);

            s_logger.Debug("Service configuration complete.");
        }


        public void ConfigureContainer(object obj)
        {
            s_logger.Debug("Configuring dependency injection container...");

            if (obj is ContainerBuilder builder)
            {
                builder.RegisterModule(new TakHubServiceRegistrar());
                builder.RegisterModule(new InfrastructureServiceRegistrar());
                builder.RegisterModule(new CoreServiceRegistrar());

                builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                    .Where(t => t.Name.EndsWith("Presenter")).SingleInstance();
            }
            else if (obj is IServiceCollection services)
            {
                services.LoadTakHubServices();
                services.LoadInfrastructureServices();
                services.LoadCoreServices();

                services.AddSingleton(typeof(ExchangeRefreshTokenPresenter));
                services.AddSingleton(typeof(LoginPresenter));
                services.AddSingleton(typeof(LogoutPresenter));
                services.AddSingleton(typeof(RegisterUserPresenter));
            }

            s_logger.Debug("Dependency injection container configuration complete.");
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            s_logger.Debug("Configuring application...");

            app.UseExceptionHandler(builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    if (error != null)
                    {
                        s_logger.Debug($"Exception thrown during request processing: {error.Error.Message}");
                        context.Response.AddApplicationError(error.Error.Message);
                        await context.Response.WriteAsync(error.Error.Message).ConfigureAwait(false);
                    }
                });
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "TakHub V1"); });
            }

            app.UseRouting();
            app.UseAuthentication();    // Must be done AFTER UseRouting.
            app.UseAuthorization();     // Must be done AFTER UseAuthentication.
            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "takhub/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("api", "takhub/api/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<GameHub>("/takhub/gamehub");
            });

            app.UseWelcomePage();

            s_logger.Debug("Application configuration complete.");
        }


        private void InitializeEngine()
        {
            var settings = Configuration.GetSection("takHubFramework").Get<TakHubFrameworkSettings>();

            if (settings.UseActorSystem)
            {
                string pathName = Path.IsPathRooted(settings.ActorConfigFile) ? settings.ActorConfigFile
                                : Path.Combine(Program.GetApplicationDirectory(), settings.ActorConfigFile);
                AkkaSystem.Initialize(File.ReadAllText(pathName), settings.ActorSystemAddress);
            }

            AIConfiguration<TakAIOptions>.Initialize(Configuration);

        }


        private void ConfigureTakHubServices(IServiceCollection services)
        {
            var frameworkSettings = Configuration.GetSection("takHubFramework");
            services.Configure<TakHubFrameworkSettings>(frameworkSettings);
        }


        private void ConfigureDatabaseServices(IServiceCollection services)
        {
            string databaseProvider = (Configuration["databaseProvider"] ?? "sqlServer").ToLower();

            if (databaseProvider == "sqlite")
            {
                s_logger.Debug("Using SQLite database provider; all changes to the database will be persisted.");

                AddSqliteContext<AppIdentityDbContext>(services, "sqlite");
                AddSqliteContext<AppDbContext>        (services, "sqlite");
            }
            else if (databaseProvider == "sqlserver")
            {
                s_logger.Debug("Using SQL database provider; all changes to the database will be persisted.");

                AddSqlServerContext<AppIdentityDbContext>(services, "sqlserver");
                AddSqlServerContext<AppDbContext>        (services, "sqlserver");
            }
            else
            {
                string message = "You must specify either \"sqlite\" or \"sqlserver\" value for the "
                                                                    + "\"databaseProvider\" setting.";
                s_logger.Error(message);
                throw new Exception(message);
            }
        }


        private static void ConfigureIdentityServices(IServiceCollection services)
        {
            var identityBuilder = services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit           = false;
                options.Password.RequireLowercase       = false;
                options.Password.RequireUppercase       = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength         = 8;
            });

            identityBuilder = new IdentityBuilder(identityBuilder.UserType, typeof(IdentityRole),
                                                                       identityBuilder.Services);
            identityBuilder.AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();
        }


        private void ConfigureAuthenticationServices(IServiceCollection services)
        {
            // Register the ConfigurationBuilder instance of AuthSettings.
            var authSettings = Configuration.GetSection(nameof(AuthSettings));
            services.Configure<AuthSettings>(authSettings);

            var secretKey   = Encoding.ASCII.GetBytes(authSettings[nameof(AuthSettings.SecretKey)]);
            var signingKey  = new SymmetricSecurityKey(secretKey);
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var jwtIssuerOptions = Configuration.GetSection(nameof(JwtIssuerOptions));
            var validFor         = jwtIssuerOptions[nameof(JwtIssuerOptions.ValidFor)];
            var audience         = jwtIssuerOptions[nameof(JwtIssuerOptions.Audience)];
            var issuer           = jwtIssuerOptions[nameof(JwtIssuerOptions.Issuer)];

            var duration = (validFor != null) ? TimeSpan.Parse(validFor, new CultureInfo("en-US"))
                                              : TimeSpan.FromMinutes(120);

            services.Configure<JwtIssuerOptions>(options =>
            {
                options.SigningCredentials = credentials;
                options.ValidFor           = duration;
                options.Audience           = audience;
                options.Issuer             = issuer;
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime         = true,
                ValidIssuer              = issuer,
                ValidAudience            = audience,
                IssuerSigningKey         = signingKey,
                RequireExpirationTime    = false,
                ClockSkew                = TimeSpan.Zero
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(configureOptions =>
            {
                configureOptions.TokenValidationParameters = tokenValidationParameters;
                configureOptions.ClaimsIssuer              = issuer;
                configureOptions.SaveToken                 = true;

                configureOptions.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiUser", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim(Constants.Strings.JwtClaimIdentifiers.Rol,
                                        Constants.Strings.JwtClaims.ApiAccess);
                });
            });
        }


        private static void ConfigureTypeMappingServices(IServiceCollection services)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.ManifestModule.Name.StartsWith("STak.TakHub")).ToArray();
            services.AddAutoMapper(assemblies);
        }


        private static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TakHub", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please insert JWT with Bearer into field",
                    In          = ParameterLocation.Header,
                    Type        = SecuritySchemeType.ApiKey,
                    Name        = "Authorization"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Scheme = "Bearer" }, Array.Empty<string>()
                    }
                });
            });
        }


        private static void ConfigureMvcServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.EnableEndpointRouting = false;
            })
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>());
        }


        private void ConfigureSignalRServices(IServiceCollection services)
        {
            var frameworkOptions = new AspNetFrameworkOptions();
            Configuration.GetSection("AspNetFramework").Bind(frameworkOptions);

            var signalROptions = frameworkOptions.SignalR;
            var timeout        = signalROptions.ClientTimeout;
            var protocol       = signalROptions.Protocol;

            var signalR = services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;      // TODO - Change to false before releasing.
                options.ClientTimeoutInterval = timeout;
            });

#if SYSTEMTEXT_JSON_SERIALIZATION
            if (protocol == "SystemTextJson")
            {
                signalR.AddJsonProtocol(options =>
                {
                    // Options to be added when/if required.
                });
            }
#endif

#if NEWTONSOFT_JSON_SERIALIZATION
            if (protocol == "NewtonsoftJson")
            {
                var resolver = new DefaultContractResolver
                {
                    IgnoreSerializableAttribute = false
                };
                signalR.AddNewtonsoftJsonProtocol(options =>
                {
                    options.PayloadSerializerSettings.TypeNameHandling = TypeNameHandling.All;
                    options.PayloadSerializerSettings.ContractResolver = resolver;
                });
            }
#endif

#if MESSAGEPACK_SERIALIZATION
            if (protocol == "MessagePack")
            {
                signalR.AddMessagePackProtocol(options =>
                {
                    options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard;
                });
            }
#endif
        }


        private void AddSqliteContext<TContext>(IServiceCollection services, string context)
            where TContext : DbContext
        {
            string fileName = null;
            string connectStr = Configuration.GetConnectionString(context);

            var match = Regex.Match(connectStr, "(?<prefix>.*)DataSource=(?<filename>[^;]*)(?<suffix>.*)",
                                                                                 RegexOptions.IgnoreCase);
            if (match.Success)
            {
                fileName = match.Groups["filename"].Value;
                if (! fileName.Contains('\\') && ! fileName.Contains('/'))
                {
                    fileName = Path.Combine(Program.GetDatabaseDirectory(), fileName);
                    string prefix = match.Groups["prefix"].Value;
                    string suffix = match.Groups["suffix"].Value;
                    connectStr = $"{prefix}DataSource={fileName}{suffix}";
                }

                string databaseDir = Path.GetDirectoryName(fileName);
                if (! Directory.Exists(databaseDir))
                {
                    Directory.CreateDirectory(databaseDir);
                }
            }

            string migrationAssembly = "STak.TakHub.Infrastructure";
            services.AddDbContext<TContext>(options => options.UseSqlite(connectStr,
                                        b => b.MigrationsAssembly(migrationAssembly)));
        }


        private void AddSqlServerContext<TContext>(IServiceCollection services, string context)
            where TContext : DbContext
        {
            string migrationAssembly = Assembly.GetExecutingAssembly().FullName;
            string connectionString = Configuration.GetConnectionString(context);

            services.AddDbContext<TContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly(migrationAssembly)));
        }
    }
}
