using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    //Request from CustomerWallet with new Improvement
    public class CustomerServiceRequestDTO
    {
        public CustomerDTO PrimaryCustomer { get; set; }
        public string CustomerComment { get; set; }
        public DateTime StartProjectDate { get; set; }
        public IList<ServiceRequestDTO> ServiceRequests { get; set; }
        // Lead source of a client web-portal (DP, MB, OB, CW)
        public string LeadSource { get; set; }
        //If customer have own contractor
        public ContractorDTO ContractorInfo { get; set; }
    }
}
