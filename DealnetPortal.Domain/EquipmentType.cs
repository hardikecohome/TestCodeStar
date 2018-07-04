using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class EquipmentType
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string DescriptionResource { get; set; }
        public bool UnderBill59 { get; set; }
        public bool Leased { get; set; }
        //Userful Life of equipment (years)
        public int UsefulLife { get; set; }
        public decimal? SoftCap { get; set; }
        public decimal? HardCap { get; set; }
        public virtual ICollection<EquipmentSubType> SubTypes { get; set; }
    }
}
