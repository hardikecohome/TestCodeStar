using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class ServiceRequestDTO
    {
        public string DealerId { get; set; }
        public string DealerName { get; set; }
        public int? PrecreatedContractId { get; set; }
        public string PrecreatedContractTransactionId { get; set; }
        public string ServiceType { get; set; }
    }
}
