using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract.EquipmentInformation
{
    public class InstallationPackageDTO
    {
        public int Id { get; set; }        
        public string Description { get; set; }
        public decimal? MonthlyCost { get; set; }        
    }
}
