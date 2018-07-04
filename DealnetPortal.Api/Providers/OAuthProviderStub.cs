using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;

namespace DealnetPortal.Api.Providers
{
    public class OAuthProviderStub : OAuthAuthorizationServerProvider
    {
        private const string DefaultClientId = "123";
        private const string DefaultClientName = "TestUser";

        public OAuthProviderStub()
        {

        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            await Task.Run(() => 
            {
                var clientName = context.UserName ?? DefaultClientName;
                var oAuthIdentity = new ClaimsIdentity(OAuthDefaults.AuthenticationType);
                var nameIdentifier = Guid.NewGuid().ToString();
                oAuthIdentity.AddClaim(new Claim(ClaimTypes.Name, clientName));
                oAuthIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
                ClaimsIdentity cookiesIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationType);
                cookiesIdentity.AddClaim(new Claim(ClaimTypes.Name, clientName));
                cookiesIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
                IDictionary<string, string> data = new Dictionary<string, string>
                {
                    { "userName", clientName }
                };
                AuthenticationProperties properties = new AuthenticationProperties(data);
                var ticket = new AuthenticationTicket(oAuthIdentity, properties);
                context.Validated(ticket);
                context.Request.Context.Authentication.SignIn(cookiesIdentity);
            });
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }
    }
}