using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class DealerEquipment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ProfileId { get; set; }
        [ForeignKey("ProfileId")]
        public virtual DealerProfile DealerProfile { get; set; }

        public int EquipmentId { get; set; }
        [ForeignKey("EquipmentId")]
        public virtual EquipmentType Equipment { get; set; }
    }
}
