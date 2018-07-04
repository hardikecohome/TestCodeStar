using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Models.DealerOnboarding
{
    public class DealerInfoDTO
    {
        public int Id { get; set; }
        public string AccessKey { get; set; }

        /// <summary>
        /// part of a unique sales rep link
        /// </summary>
        public string SalesRepLink { get; set; }

        public string ParentSalesRepId { get; set; }        

        public string TransactionId { get; set; }

        public bool MarketingConsent { get; set; }
        public bool CreditCheckConsent { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }

        public CompanyInfoDTO CompanyInfo { get; set; }
        public List<OwnerInfoDTO> Owners { get; set; }
        public ProductInfoDTO ProductInfo { get; set; }
        public List<RequiredDocumentDTO> RequiredDocuments { get; set; }
        public List<AdditionalDocumentDTO> AdditionalDocuments { get; set; }

        // Lead source of a client web-portal (DP, MB, OB, CW)
        public string LeadSource { get; set; }
    }
}
