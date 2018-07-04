using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerRiskGroupDTO
    {
        public int Id { get; set; }

        public string GroupName { get; set; }

        public int BeaconScoreFrom { get; set; }

        public int BeaconScoreTo { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public bool CapOutMaxAmt { get; set; }
    }
}
