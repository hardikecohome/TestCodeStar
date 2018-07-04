using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class EquipmentSubType
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Description { get; set; }
        public string DescriptionResource { get; set; }
        public decimal? SoftCap { get; set; }
        public decimal? HardCap { get; set; }
        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public EquipmentType ParentEquipmentType { get; set; }
    }
}
