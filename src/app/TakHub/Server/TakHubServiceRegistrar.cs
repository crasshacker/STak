using Microsoft.Extensions.DependencyInjection;
using Autofac;
using STak.TakHub.Hubs;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakEngine;
using STak.TakEngine.Management;

namespace STak.TakHub.Helpers
{
    public class TakHubServiceRegistrar : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new GameManager()).As<GameManager>();
            builder.RegisterType<HubGameService>().As<IHubGameService>().SingleInstance();
            builder.RegisterType<SignalRGameHubContext>().As<IGameHubContext>().SingleInstance();
        }
    }


    public static class TakHubServiceExtensions
    {
        public static void LoadTakHubServices(this IServiceCollection services)
        {
            services.AddSingleton(new GameManager());
            services.AddSingleton(typeof(IHubGameService), typeof(HubGameService));
            services.AddSingleton(typeof(IGameHubContext), typeof(SignalRGameHubContext));
        }
    }
}
