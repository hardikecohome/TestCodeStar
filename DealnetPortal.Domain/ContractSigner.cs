using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    public class ContractSigner
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ContractId { get; set; }
        [ForeignKey("ContractId")]
        [Required]
        public Contract Contract { get; set; }
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [EmailAddress]
        public string EmailAddress { get; set; }
        public SignatureRole SignerType { get; set; }
        public SignatureStatus? SignatureStatus { get; set; }
        public string SignatureStatusQualifier { get; set; }
        public DateTime? StatusLastUpdateTime { get; set; }
        public string Comment { get; set; }
    }
}
