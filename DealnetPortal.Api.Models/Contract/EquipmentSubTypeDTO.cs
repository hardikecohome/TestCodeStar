using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class EquipmentSubTypeDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal? SoftCap { get; set; }
        public decimal? HardCap { get; set; }
        public EquipmentTypeDTO ParenEquipmentType { get; set; }
    }
}
