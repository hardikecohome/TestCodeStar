using System.Collections.Generic;

namespace DealnetPortal.Api.Models.Contract
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public bool EsignatureEnabled { get; set; }

        public IList<ApplicationUserDTO> SubDealers { get; set; }
        /// <summary>
        /// Sub-dealers from aspire tables
        /// </summary>
        public IList<SubDealerDTO> UdfSubDealers { get; set; }
    }
}
