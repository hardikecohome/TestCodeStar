using System.Collections.Generic;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    /// <summary>
    /// Options set for selected dealer and language
    /// </summary>
    public class CustomerLinkLanguageOptionsDTO
    {
        /// <summary>
        /// Is requested language available?
        /// </summary>
        public bool IsLanguageEnabled { get; set; }
        /// <summary>
        /// Codes of enabled languages
        /// </summary>
        public List<LanguageCode> EnabledLanguages { get; set; }
        /// <summary>
        /// Services for selected language
        /// </summary>
        public List<string> LanguageServices { get; set; }
        /// <summary>
        /// Services for selected language
        /// </summary>
        public string DealerName { get; set; }

        public bool QuebecDealer { get; set; }
    }
}
