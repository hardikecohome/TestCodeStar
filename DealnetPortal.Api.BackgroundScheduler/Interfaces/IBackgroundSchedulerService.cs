using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.BackgroundScheduler
{
    public interface IBackgroundSchedulerService
    {
        void CheckExpiredLeads(DateTime currentDateTime, int minutesPeriod);
    }
}
