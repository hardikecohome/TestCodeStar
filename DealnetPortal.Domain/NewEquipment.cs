namespace DealnetPortal.Domain
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class NewEquipment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Type { get; set; }

        public int? EquipmentTypeId { get; set; }
        [ForeignKey("EquipmentTypeId")]
        public virtual EquipmentType EquipmentType { get; set; }

        public int? EquipmentSubTypeId { get; set; }
        [ForeignKey("EquipmentSubTypeId")]
        public virtual EquipmentSubType EquipmentSubType { get; set; }

        public string Description { get; set; }
        public decimal? Cost { get; set; }
        public decimal? MonthlyCost { get; set; }
        public decimal? EstimatedRetailCost { get; set; }
        public DateTime? EstimatedInstallationDate { get; set; }
        public string AssetNumber { get; set; }

        public string InstalledSerialNumber { get; set; }
        public string InstalledModel { get; set; }

        public bool? IsDeleted { get; set; }

        public int EquipmentInfoId { get; set; }
        [ForeignKey("EquipmentInfoId")]
        public EquipmentInfo EquipmentInfo { get; set; }
    }
}
