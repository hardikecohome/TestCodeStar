using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.DataAccess
{
    public class SecureDbContext : ApplicationDbContext
    {
        public SecureDbContext()
        {
            SetupSecureDb();
        }

        #region private
        private void SetupSecureDb()
        {
            try
            {
                Crypteron.CipherDb.Session.Create(this);
            }
            catch (Exception ex)
            {
                //Logging exception of secure context creation here
                Crypteron.ErrorHandling.Logging.Logger.Log($"Cannot create secure DB context: {ex}", TraceEventType.Error);
            }
        }
        #endregion
    }
}
