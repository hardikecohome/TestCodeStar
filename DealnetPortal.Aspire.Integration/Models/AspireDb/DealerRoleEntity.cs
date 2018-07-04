using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Aspire.Integration.Models.AspireDb
{
    public class DealerRoleEntity : Entity
    {        
        public string Fax { get; set; }

        public DateTime? StatusDate { get; set; }
        public string ProductType { get; set; }
        public string ChannelType { get; set; }
        public string Ratecard { get; set; }
        public string LeaseRatecard { get; set; }
        public string Role { get; set; }
        public string UserId { get; set; }
        public bool? DisplayTooltips { get; set; }
    }
}
