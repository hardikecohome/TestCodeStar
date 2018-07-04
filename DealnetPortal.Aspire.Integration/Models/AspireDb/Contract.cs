using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Aspire.Integration.Models.AspireDb
{
    public class Contract
    {
        public long TransactionId { get; set; }
        public string DealStatus { get; set; }
        public string AgreementType { get; set; }        
        public int Term { get; set; }
        public decimal AmountFinanced { get; set; }

        public string EquipmentDescription { get; set; }
        public string EquipmentType { get; set; }

        public string CustomerAccountId { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }

        public string LastUpdateDate { get; set; }
        public string LastUpdateTime { get; set; }
        public DateTime LastUpdateDateTime { get; set; }
        
        public string OverrideCustomerRiskGroup { get; set; }
        public decimal? OverrideCreditAmountLimit { get; set; }
    }
}
