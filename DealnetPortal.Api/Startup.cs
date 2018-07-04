using System;
using System.Diagnostics;
using Hangfire;
using Microsoft.Owin;
using Owin;
using System.Configuration;

[assembly: OwinStartup(typeof(DealnetPortal.Api.Startup))]

namespace DealnetPortal.Api
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ConfigurationScheduler(app);
        }
    }
}
