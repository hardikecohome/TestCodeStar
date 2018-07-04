using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ObjectBuilder2;

namespace DealnetPortal.Utilities.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        private readonly ICollection<string> _additionalSections;

        public AppConfiguration()
        {
            _additionalSections = null;
        }
        public AppConfiguration(ICollection<string> additionalSections)
        {
            _additionalSections = additionalSections;
        }

        public string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? SearchValue(key);
        }

        private string SearchValue(string key)
        {
            return _additionalSections?.Select(s => (ConfigurationManager.GetSection(s) as NameValueCollection))
                    .FirstOrDefault(s => s?[key] != null)?[key];
        }
    }
}
