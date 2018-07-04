using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract.EquipmentInformation
{
    public class ExistingEquipmentDTO
    {
        public int Id { get; set; }
        public bool IsRental { get; set; }
        
        public string RentalCompany { get; set; }
        
        public double EstimatedAge { get; set; }
        
        public string Make { get; set; }
        
        public string Model { get; set; }
        
        public string SerialNumber { get; set; }
        
        public string GeneralCondition { get; set; }
        
        public string Notes { get; set; }

        public ResponsibleForRemovalType? ResponsibleForRemoval { get; set; }
        public string ResponsibleForRemovalValue { get; set; }
    }
}
