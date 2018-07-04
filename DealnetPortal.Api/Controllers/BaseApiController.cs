using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using DealnetPortal.Domain;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;
using Microsoft.AspNet.Identity;

namespace DealnetPortal.Api.Controllers
{
    public class BaseApiController : ApiController
    {
        /// <summary>
        /// LoggingService - the <see cref="ILoggingService"/> used to log errors and messages in the controllers
        /// </summary>
        protected ILoggingService LoggingService { get; set; }

        protected BaseApiController(ILoggingService loggingService)
        {
            if (loggingService == null)
                throw new ArgumentNullException("loggingService");

            LoggingService = loggingService;
        }

        private LoggedInUser _loggedInUser;
        /// <summary>
        /// Logged in user based on bearer token
        /// </summary>
        protected LoggedInUser LoggedInUser
        {
            get
            {
                if (_loggedInUser == null)
                {
                    var identity = (ClaimsIdentity)RequestContext.Principal.Identity;
                    //var identity = (ClaimsIdentity)User.Identity;

                    if (identity == null || identity.Claims.All(t => t.Type != ClaimTypes.Name)) // claims types name must be defined in token, otherwise we will not be able to get username
                        return null;

                    _loggedInUser = new LoggedInUser(identity.GetUserName(), identity.GetUserId());
                }

                return _loggedInUser;
            }
        }
    }
}
