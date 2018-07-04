using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class Tier
    {
        public Tier()
        {
            RateCards = new HashSet<RateCard>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }

        public bool? PassAdminFee { get; set; }

        public bool? IsCustomerRisk { get; set; }

        public ICollection<RateCard> RateCards { get; set; }
    }
}
