using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using DealnetPortal.Api.Providers;
using DealnetPortal.DataAccess;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {            
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            AuthType authType;
            if (
                Enum.TryParse(ConfigurationManager.AppSettings.Get(WebConfigKeys.AUTHPROVIDER_CONFIG_KEY), out authType) ==
                false)
            {
                authType = AuthType.AuthProvider;                
            }

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),                
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                // In production mode set AllowInsecureHttp = false
                AllowInsecureHttp = true
            };

            switch (authType)
            {                
                case AuthType.Stub:
                    OAuthOptions.Provider = new OAuthProviderStub();
                    break;
                case AuthType.Aspire:
                case AuthType.AuthProvider:
                default:
                    // Configure the db context and user manager to use a single instance per request                    
                    app.CreatePerOwinContext(ApplicationDbContext.Create);
                    app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
                    app.CreatePerOwinContext<ApplicationRoleManager>(ApplicationRoleManager.Create);
                    OAuthOptions.Provider = new ApplicationOAuthProvider(PublicClientId);
                    break;                    
            }

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            //app.UseFacebookAuthentication(
            //    appId: "",
            //    appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
    }
}
