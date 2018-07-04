using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Signature
{
    public class SignatureSummaryDTO
    {
        public int ContractId { get; set; }

        public string SignatureTransactionId { get; set; }

        public SignatureStatus? Status { get; set; }

        public string StatusQualifier { get; set; }

        public DateTime? StatusTime { get; set; }

        public List<ContractSignerDTO> Signers { get; set; }
    }
}
