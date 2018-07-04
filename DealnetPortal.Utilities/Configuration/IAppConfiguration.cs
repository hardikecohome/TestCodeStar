using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Utilities.Configuration
{
    public interface IAppConfiguration
    {
        string GetSetting(string key);
    }
}
