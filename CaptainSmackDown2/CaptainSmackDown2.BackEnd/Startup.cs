using System.Web.Http;
using Owin;
using Microsoft.ServiceFabric.Data;
using Microsoft.Practices.Unity;
using Unity.WebApi;
using CaptainSmackDown2.BackEnd.Controllers;

namespace CaptainSmackDown2.BackEnd
{
    public static class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder, IReliableStateManager stateManager)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            UnityContainer container = new UnityContainer();

            container.RegisterType<VoteController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(stateManager));

            config.DependencyResolver = new UnityDependencyResolver(container);

            config.MapHttpAttributeRoutes();

            appBuilder.UseWebApi(config);
        }
    }
}
