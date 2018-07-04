using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Contract;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using DealnetPortal.Domain;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;
using Microsoft.AspNet.Identity;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private IAspireService _aspireService;
        private IUsersService _usersService;
        private ILoggingService _loggingService;
        private readonly string _publicClientId;
        private AuthType _authType;

        public ApplicationOAuthProvider(string publicClientId)
        {
            _aspireService = (IAspireService) GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IAspireService));
            _usersService = (IUsersService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IUsersService));
            _loggingService = (ILoggingService) GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(ILoggingService));

            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;

            if (!Enum.TryParse(ConfigurationManager.AppSettings.Get(WebConfigKeys.AUTHPROVIDER_CONFIG_KEY), out _authType))
            {
                _authType = AuthType.AuthProvider;
            }
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            _aspireService = (IAspireService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IAspireService));
            _usersService = (IUsersService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IUsersService));
            _loggingService = (ILoggingService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(ILoggingService));

            ApplicationUser user = null;
            try
            {            

                user = await userManager.FindAsync(context.UserName, context.Password);
            }
            catch (Exception ex)
            {

                //throw;
            }

            if (user == null || !string.IsNullOrEmpty(user.AspireLogin))
            {
                var aspireRes = await CheckAndAddOrUpdateAspireUser(context);
                if (aspireRes?.Item2?.Any(e => e.Type == AlertType.Error) ?? false)
                {
                    user = null;
                    if (aspireRes?.Item2?.Any(e => e.Code == ErrorCodes.AspireConnectionFailed) ?? false)
                    {
                        context.SetError(ErrorConstants.ServiceFailed, Resources.Resources.ExternalServiceUnavailable);
                        return;
                    }                    
                }
                else
                {
                    user = aspireRes?.Item1;
                }
            }            

            if (user == null)
            {
                context.SetError(ErrorConstants.InvalidGrant, Resources.Resources.UserNamerPasswordIncorrect);
                return;
            }

            var applicationId = context.OwinContext.Get<string>("portalId");

            if (user.ApplicationId != applicationId)
            {
                context.SetError(ErrorConstants.UnknownApplication, Resources.Resources.UnknownApplicationToLogIn);
                return;
            }

            if (_authType != AuthType.AuthProviderOneStepRegister && !user.EmailConfirmed)
            {
                context.SetError(ErrorConstants.ResetPasswordRequired, Resources.Resources.OntimePassCorrectNowChange);
                return;
            }

            //TODO: special clames and other headers info can be added here
            //context.OwinContext.Response.Headers.Append("user", "userHeader");

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);                        

            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);

            var claims = _usersService.GetUserClaims(user);            
            
            if (claims?.Any() ?? false)
            {
                oAuthIdentity.AddClaims(claims);
                cookiesIdentity.AddClaims(claims);
            }
            //update user roles
            if (claims?.Any() ?? false)
            {
                var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
                if (roles.Any())
                {
                    try
                    {
                        await userManager.AddToRolesAsync(user.Id, roles);
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError($"Cannot set roles for user [{user.UserName}]", ex);
                    }
                }
            }
            userManager.GetRoles(user.Id)?.ForEach(r => claims?.Add(new Claim(ClaimTypes.Role, r)));

            AuthenticationProperties properties = CreateProperties(user.UserName, claims);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            var portalId = context.Parameters.Where(f => f.Key == "portalId").Select(f => f.Value).FirstOrDefault()?.FirstOrDefault();
            if (!string.IsNullOrEmpty(portalId))
            {
                context.OwinContext.Set<string>("portalId", portalId);
            }
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName, IEnumerable<Claim> claims = null)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            claims?.Where(c => c.Type != ClaimTypes.Role).ForEach(claim =>
            {
                data.Add($"claim:{claim.Type}", claim.Value);
            });
            if (claims?.Any(c => c.Type == ClaimTypes.NameIdentifier) ?? false)
            {
                data.Add("userId", claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
            }
            var roles = string.Join(":", claims?.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray());
            if (!string.IsNullOrEmpty(roles))
            {
                data.Add("roles", roles);
            }
            return new AuthenticationProperties(data);
        }

        private async Task<Tuple<ApplicationUser, IList<Alert>>> CheckAndAddOrUpdateAspireUser(OAuthGrantResourceOwnerCredentialsContext context)
        {
            ApplicationUser user = null;
            List<Alert> outAlerts = new List<Alert>();

            if (_aspireService != null)
            {
                _loggingService?.LogInfo($"Check user [{context.UserName}] in Aspire");
                var alerts = await _aspireService.LoginUser(context.UserName, context.Password);
                if (alerts?.Any() ?? false)
                {
                    outAlerts.AddRange(alerts);
                }
                
                if (alerts?.All(a => a.Type != AlertType.Error) ?? false)
                {                    
                    var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();
                    var applicationId = context.OwinContext.Get<string>("portalId");
                    var oldUser = await userManager.FindByNameAsync(context.UserName);
                 
                    if (oldUser != null)
                    {                        
                        user = oldUser;//await userManager.FindAsync(context.UserName, context.Password);
                    }
                    else
                    {
                        var newUser = new ApplicationUser()
                        {
                            UserName = context.UserName,
                            Email = "",
                            ApplicationId = applicationId,
                            EmailConfirmed = true,
                            TwoFactorEnabled = false,
                            AspireLogin = context.UserName,
                        };                        

                        try
                        {
                            IdentityResult result = await userManager.CreateAsync(newUser, context.Password);
                            if (result.Succeeded)
                            {
                                _loggingService?.LogInfo(
                                    $"New entity for Aspire user [{context.UserName}] created successefully");
                                user = await userManager.FindAsync(context.UserName, context.Password);
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.LogError(
                                    $"Error during create new user [{context.UserName}]:{ex.Message} ");
                            user = null;
                        }
                    }

                    if (user != null)
                    {
                        var syncAlerts = await _usersService.SyncAspireUser(user, userManager);
                        if (syncAlerts?.Any() ?? false)
                        {
                            outAlerts.AddRange(syncAlerts);
                        }

                        //check and update password
                        _usersService.UpdateUserPassword(user.Id, context.Password);
                    }
                }
                else
                {
                    alerts?.Where(a => a.Type == AlertType.Error).ForEach(a => 
                        _loggingService?.LogInfo(a.Message));
                }
            }
            return new Tuple<ApplicationUser, IList<Alert>>(user, outAlerts);
        }        
    }
}