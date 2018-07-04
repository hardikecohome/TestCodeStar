using System;
using System.Configuration;
using System.Net.Http.Formatting;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Infrastucture;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using Microsoft.Owin.Security.OAuth;

namespace DealnetPortal.Api
{
    public static class WebApiConfig
    {
        private static readonly ILoggingService _loggingService = (ILoggingService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(ILoggingService));

        public static void Register(HttpConfiguration config)
        {
            // Web API appConfiguration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));            

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Formatters.Add(new BsonMediaTypeFormatter());
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            var configReader = (IAppConfiguration)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IAppConfiguration)) ??
                                new AppConfiguration(WebConfigSections.AdditionalSections);
            bool httpsOn;
            if (!bool.TryParse(configReader.GetSetting(WebConfigKeys.HTTPS_ON_PRODUCTION_CONFIG_KEY), out httpsOn))
            {
                httpsOn = false;
            }
            if (httpsOn)
            {
                // make all web-api requests to be sent over https
                config.MessageHandlers.Add(new EnforceHttpsHandler());
            }            
        }

        public static void CheckConfigKeys()
        {            
            var configReader = (IAppConfiguration)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IAppConfiguration)) ?? 
                                new AppConfiguration(WebConfigSections.AdditionalSections);
            Type type = typeof(WebConfigKeys);
            foreach (var key in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                var keyName = key.GetValue(null).ToString();
                if (configReader.GetSetting(keyName) == null)
                {
                    _loggingService?.LogError($"{keyName} KEY DON'T EXIST IN WEB CONFIG.");
                }
            }
        }
    }
}
