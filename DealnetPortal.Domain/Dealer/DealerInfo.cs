using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain.Dealer
{
    public class DealerInfo
    {
        public DealerInfo()
        {
            Owners = new HashSet<OwnerInfo>();
            RequiredDocuments = new HashSet<RequiredDocument>();
            AdditionalDocuments = new HashSet<AdditionalDocument>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// Link to a parent dealer
        /// </summary>
        public string ParentSalesRepId { get; set; }
        [ForeignKey(nameof(ParentSalesRepId))]
        public virtual ApplicationUser ParentSalesRep { get; set; }
        /// <summary>
        /// Link to a draft for continue editing
        /// </summary>
        public string AccessKey { get; set; }

        public string TransactionId { get; set; }
        /// <summary>
        /// Status in Aspire
        /// </summary>
        public string Status { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }

        public bool Submitted { get; set; }

        public bool SentToAspire { get; set; }

        public bool MarketingConsent { get; set; }

        public bool CreditCheckConsent { get; set; }

        //public int? CompanyInfoId { get; set; }
        //[ForeignKey("CompanyInfoId")]
        public virtual CompanyInfo CompanyInfo { get; set; }

        public virtual ICollection<OwnerInfo> Owners { get; set; }

        public virtual ProductInfo ProductInfo { get; set; }

        public virtual ICollection<RequiredDocument> RequiredDocuments { get; set; }      
          
        public virtual ICollection<AdditionalDocument> AdditionalDocuments { get; set; }        
    }
}
