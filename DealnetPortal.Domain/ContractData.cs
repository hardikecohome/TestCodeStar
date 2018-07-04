using System.Collections.Generic;

namespace DealnetPortal.Domain
{
    public class ContractData
    {
        public int Id { get; set; }
        public string DealerId { get; set; }

        /// <summary>
        /// Aspire dealer for contract
        /// </summary>
        public string ExternalSubDealerName { get; set; }
        public string ExternalSubDealerId { get; set; }

        public Customer PrimaryCustomer { get; set; }

        public ContractDetails Details { get; set; }

        public IList<Customer> SecondaryCustomers { get; set; }
        public IList<Customer> HomeOwners { get; set; }
        public PaymentInfo PaymentInfo { get; set; }
        public EquipmentInfo Equipment { get; set; }

        public ContractSalesRepInfo SalesRepInfo { get; set; }
    }
}
