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
    public class DealerProfile
    {
        public DealerProfile()
        {
            Address = new Address();            
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string DealerId { get; set; }
        [ForeignKey("DealerId")]
        public virtual ApplicationUser Dealer { get; set; }

        public Address Address { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }

        public string Phone { get; set; }

        public string Culture { get; set; }

        public virtual ICollection<DealerEquipment> Equipments { get; set; }

        public virtual ICollection<DealerArea> Areas { get; set; }
    }
}
