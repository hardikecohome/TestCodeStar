using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class InstallationCertificateDataDTO
    {
        public int ContractId { get; set; }
        public List<InstalledEquipmentDTO> InstalledEquipments { get; set; }
        public string InstallerFirstName { get; set; }
        public string InstallerLastName { get; set; }
        public DateTime? InstallationDate { get; set; }
    }
}
