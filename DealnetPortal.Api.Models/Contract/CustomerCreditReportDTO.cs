using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerCreditReportDTO
    {
        public int? Beacon { get; set; }
        public bool BeaconUpdated { get; set; }
        public DateTime CreditLastUpdateTime { get; set; }

        public decimal? CreditAmount { get; set; }
        public decimal? EscalatedLimit { get; set; }
        public decimal? NonEscalatedLimit { get; set; }
    }
}
