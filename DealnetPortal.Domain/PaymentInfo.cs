using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    public class PaymentInfo
    {
        [ForeignKey("Contract")]
        public int Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public WithdrawalDateType PrefferedWithdrawalDate { get; set; }
        [MaxLength(20)]
        public string BlankNumber { get; set; }
        [MaxLength(20)]
        public string TransitNumber { get; set; }
        [MaxLength(20)]
        public string AccountNumber { get; set; }
        [MaxLength(20)]
        public string EnbridgeGasDistributionAccount { get; set; }
        [MaxLength(7)]
        public string MeterNumber { get; set; }

        public virtual Contract Contract { get; set; }
    }
}
