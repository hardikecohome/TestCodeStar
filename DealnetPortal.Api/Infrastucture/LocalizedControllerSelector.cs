using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using System.Web.Routing;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Helpers;
using Microsoft.AspNet.Identity;

namespace DealnetPortal.Api.Infrastucture
{
    public class LocalizedControllerSelector : DefaultHttpControllerSelector
    {
        public LocalizedControllerSelector(HttpConfiguration configuration) : base(configuration)
        {
        }

        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            var culture = request.Headers.AcceptLanguage.FirstOrDefault()?.Value;
            if (culture != null)
            {
                Thread.CurrentThread.CurrentCulture =
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(CultureHelper.FilterCulture(culture));
            }

            return base.SelectController(request);
        }
    }
}
