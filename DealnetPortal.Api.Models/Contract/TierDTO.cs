using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class TierDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool PassAdminFee { get; set; }

        public bool IsCustomerRisk { get; set; }

        public List<RateCardDTO> RateCards { get; set; }
    }
}
