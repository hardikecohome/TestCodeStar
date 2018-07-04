using Hangfire;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(HangFire.Startup))]

namespace BackgroundScheduler
{
    public class BackgroundScheduler
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("DefaultConnection");

            BackgroundJob.Enqueue(() => Console.WriteLine("Getting Started with HangFire!"));

            app.UseHangfireDashboard();

            app.UseHangfireServer();
        }
    }
}
