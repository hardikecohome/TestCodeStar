using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.DataAccess
{
    public class MigrateDatabaseToLatestVersionWithLog<TContext, TMigrationsConfiguration>
        : MigrateDatabaseToLatestVersion<TContext, TMigrationsConfiguration> 
        where TContext : System.Data.Entity.DbContext
        where TMigrationsConfiguration : System.Data.Entity.Migrations.DbMigrationsConfiguration<TContext>, new()
    {
        private ILoggingService _loggingService;

        public MigrateDatabaseToLatestVersionWithLog(ILoggingService loggingService)
        {
            _loggingService = loggingService;            
        }
        public override void InitializeDatabase(TContext context)
        {
            try
            {
                base.InitializeDatabase(context);
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("Failed to Initialize database", ex);
            }
        }
    }
}
