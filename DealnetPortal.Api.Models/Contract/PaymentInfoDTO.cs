using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class PaymentInfoDTO
    {
        public int Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public WithdrawalDateType PrefferedWithdrawalDate { get; set; }
        public string BlankNumber { get; set; }
        public string TransitNumber { get; set; }
        public string AccountNumber { get; set; }
        public string EnbridgeGasDistributionAccount { get; set; }
        public string MeterNumber { get; set; }
    }
}
