using Microsoft.Extensions.DependencyInjection;
using Autofac;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Infrastructure.Auth;
using STak.TakHub.Infrastructure.Data.Repositories;
using STak.TakHub.Infrastructure.Interfaces;
using STak.TakHub.Infrastructure.Logging;
using Module = Autofac.Module;

namespace STak.TakHub.Infrastructure
{
    public class InfrastructureServiceRegistrar : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var finder = new InjectionConstructorFinder();

            builder.RegisterType<UserRepository>()    .As<IUserRepository>()    .InstancePerLifetimeScope();
            builder.RegisterType<JwtFactory>()        .As<IJwtFactory>()        .SingleInstance() .FindConstructorsWith(finder);
            builder.RegisterType<JwtTokenHandler>()   .As<IJwtTokenHandler>()   .SingleInstance() .FindConstructorsWith(finder);
            builder.RegisterType<TokenFactory>()      .As<ITokenFactory>()      .SingleInstance() .FindConstructorsWith(finder);
            builder.RegisterType<JwtTokenValidator>() .As<IJwtTokenValidator>() .SingleInstance() .FindConstructorsWith(finder);
            builder.RegisterType<Logger>()            .As<ILogger>()            .SingleInstance() .FindConstructorsWith(finder);
        }
    }


    public static partial class TakHubServiceExtensions
    {
        public static void LoadInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped    (typeof(IUserRepository),    typeof(UserRepository));
            services.AddSingleton (typeof(IJwtFactory),        typeof(JwtFactory));
            services.AddSingleton (typeof(IJwtTokenHandler),   typeof(JwtTokenHandler));
            services.AddSingleton (typeof(ITokenFactory),      typeof(TokenFactory));
            services.AddSingleton (typeof(IJwtTokenValidator), typeof(JwtTokenValidator));
            services.AddSingleton (typeof(ILogger),            typeof(Logger));
        }
    }
}
