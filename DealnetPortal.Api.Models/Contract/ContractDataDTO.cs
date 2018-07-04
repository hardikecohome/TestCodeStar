using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    using EquipmentInformation;

    public class ContractDataDTO
    {
        public int Id { get; set; }
        public string DealerId { get; set; }

        /// <summary>
        /// Aspire dealer for contract
        /// </summary>
        public string ExternalSubDealerName { get; set; }
        public string ExternalSubDealerId { get; set; }

        public CustomerDTO PrimaryCustomer { get; set; }
        public IList<CustomerDTO> SecondaryCustomers { get; set; }
        public ContractDetailsDTO Details { get; set; }

        public PaymentInfoDTO PaymentInfo { get; set; }
        public EquipmentInfoDTO Equipment { get; set; }
        public ContractSalesRepInfoDTO SalesRepInfo { get; set; }
        // Lead source of a client web-portal (DP, MB, OB, CW)
        public string LeadSource { get; set; }
    }
}
