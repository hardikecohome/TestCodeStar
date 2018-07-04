using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.CustomerWallet
{
    public class TransactionInfoDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int? DealnetContractId { get; set; }
        public string AspireAccountId { get; set; }
        public string AspireTransactionId { get; set; }
        public string AspireStatus { get; set; }
        public int? ScorecardPoints { get; set; }
        public decimal? CreditAmount { get; set; }
        public string EquipmentType { get; set; }
        public string DealerName { get; set; }
        public DateTime? UpdateTime { get; set; }
        public bool? IsIncomplete { get; set; }
        public string CustomerComment { get; set; }
        public DateTime? CreationDate { get; set; }
    }
}
