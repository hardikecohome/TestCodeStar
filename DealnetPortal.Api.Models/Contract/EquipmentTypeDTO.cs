using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class EquipmentTypeDTO
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool UnderBill59 { get; set; }
        public bool Leased { get; set; }
        public int UsefulLife { get; set; }
        public decimal? SoftCap { get; set; }
        public decimal? HardCap { get; set; }
        public List<EquipmentSubTypeDTO> SubTypes { get; set; }
    }
}
