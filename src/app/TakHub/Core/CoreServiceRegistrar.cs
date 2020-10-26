using Microsoft.Extensions.DependencyInjection;
using Autofac;
using STak.TakHub.Core.Interfaces.UseCases;
using STak.TakHub.Core.UseCases;

namespace STak.TakHub.Core
{
    public class CoreServiceRegistrar : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RegisterUserUseCase>().As<IRegisterUserUseCase>().InstancePerLifetimeScope();
            builder.RegisterType<LoginUseCase>().As<ILoginUseCase>().InstancePerLifetimeScope();
            builder.RegisterType<LogoutUseCase>().As<ILogoutUseCase>().InstancePerLifetimeScope();
            builder.RegisterType<ExchangeRefreshTokenUseCase>().As<IExchangeRefreshTokenUseCase>().InstancePerLifetimeScope();
        }
    }


    public static partial class TakHubServiceExtensions
    {
        public static void LoadCoreServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRegisterUserUseCase), typeof(RegisterUserUseCase));
            services.AddScoped(typeof(ILoginUseCase), typeof(LoginUseCase));
            services.AddScoped(typeof(ILogoutUseCase), typeof(LogoutUseCase));
            services.AddScoped(typeof(IExchangeRefreshTokenUseCase), typeof(ExchangeRefreshTokenUseCase));
        }
    }
}
