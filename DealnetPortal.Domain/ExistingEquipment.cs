using System.ComponentModel.DataAnnotations;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class ExistingEquipment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public bool IsRental { get; set; }
        
        public string RentalCompany { get; set; }
       
        public double EstimatedAge { get; set; }
       
        public string Make { get; set; }
        
        public string Model { get; set; }
        
        public string SerialNumber { get; set; }
        
        public string GeneralCondition { get; set; }
        
        public string Notes { get; set; }

        public int EquipmentInfoId { get; set; }
        [ForeignKey("EquipmentInfoId")]
        public EquipmentInfo EquipmentInfo { get; set; }

        public ResponsibleForRemovalType? ResponsibleForRemoval { get; set; }
        [MaxLength(20)]
        public string ResponsibleForRemovalValue { get; set; }
    }
}
