using System.Web.Http;
using Owin;
using Swashbuckle.Application;

namespace CaptainSmackDown2.Api
{
    public static class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            //Add the Swashbuckle.Core NuGet Package (the full Swashbuckle package has a dependency on System.web
            //Use the below command to wire it into the Owin pipeline
            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "WebAPI");
            }).EnableSwaggerUi();

            appBuilder.UseWebApi(config);
        }
    }
}
