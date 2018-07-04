using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class InstalledEquipmentDTO
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
    }
}
