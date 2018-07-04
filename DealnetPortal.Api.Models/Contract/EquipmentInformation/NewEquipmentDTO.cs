using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract.EquipmentInformation
{
    public class NewEquipmentDTO
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public EquipmentTypeDTO EquipmentType { get; set; }
        public EquipmentSubTypeDTO EquipmentSubType { get; set; }
        public string TypeDescription { get; set; }
        public string Description { get; set; }
        public decimal? Cost { get; set; }
        public decimal? MonthlyCost { get; set; }
        public decimal? EstimatedRetailCost { get; set; }
        public DateTime? EstimatedInstallationDate { get; set; }
        public string AssetNumber { get; set; }
        public string InstalledSerialNumber { get; set; }
        public string InstalledModel { get; set; }
    }
}
