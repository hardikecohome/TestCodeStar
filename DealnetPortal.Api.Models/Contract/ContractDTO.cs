using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Models.Signature;

namespace DealnetPortal.Api.Models.Contract
{
    using EquipmentInformation;

    public class ContractDTO
    {
        public ContractDTO()
        {
            SecondaryCustomers = new List<CustomerDTO>();
        }
        public int Id { get; set; }               

        public string DealerId { get; set; }

        public string DealerName { get; set; }

        public ContractState ContractState { get; set; }       

        public DateTime CreationTime { get; set; }

        public DateTime? LastUpdateTime { get; set; }

        public CustomerDTO PrimaryCustomer { get; set; }

        /// <summary>
        /// Aspire dealer for contract
        /// </summary>
        public string ExternalSubDealerName { get; set; }
        public string ExternalSubDealerId { get; set; }
        public List<CustomerDTO> SecondaryCustomers { get; set; }
        public ContractDetailsDTO Details { get; set; }

        public PaymentInfoDTO PaymentInfo { get; set; }

        public EquipmentInfoDTO Equipment { get; set; }

        public ContractSalesRepInfoDTO SalesRepInfo { get; set; }

        public List<CommentDTO> Comments { get; set; }
        
        public List<ContractDocumentDTO> Documents { get; set; }

        public bool? WasDeclined { get; set; }

        public bool? IsCreatedByCustomer { get; set; }

        public bool? IsNewlyCreated { get; set; }

        public bool? OnCreditReview { get; set; }

        public List<ContractSignerDTO> Signers { get; set; }
    }
}
