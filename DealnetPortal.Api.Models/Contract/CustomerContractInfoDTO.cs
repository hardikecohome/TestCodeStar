using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerContractInfoDTO
    {
        public int ContractId { get; set; }
        public string TransactionId { get; set; }
        public string AccountId { get; set; }
        public string DealerName { get; set; }
        public LocationDTO DealerAdress { get; set; }
        public string DealerPhone { get; set; }
        public string DealerEmail { get; set; }
        public string DealerType { get; set; }

        public decimal CreditAmount { get; set; }
        public int ScorecardPoints { get; set; }
        public int? CustomerBeacon { get; set; }
        public string Status { get; set; }
        public IList<string> EquipmentTypes { get; set; }
        public ContractState ContractState { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public bool IsPreApproved { get; set; }
    }
}
