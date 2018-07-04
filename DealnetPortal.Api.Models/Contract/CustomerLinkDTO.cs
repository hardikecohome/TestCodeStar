using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerLinkDTO
    {
        /// <summary>
        /// Codes of enabled languages
        /// </summary>
        public List<LanguageCode> EnabledLanguages { get; set; }
        public Dictionary<LanguageCode, List<string>> Services { get; set; }
        public string HashLink { get; set; }
    }
}
