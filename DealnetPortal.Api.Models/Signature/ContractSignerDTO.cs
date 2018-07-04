using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Signature
{
    public class ContractSignerDTO
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }        
        public SignatureRole SignerType { get; set; }
        public SignatureStatus? SignatureStatus { get; set; }
        public string SignatureStatusQualifier { get; set; }
        public DateTime? StatusLastUpdateTime { get; set; }
        public string Comment { get; set; }
    }
}
