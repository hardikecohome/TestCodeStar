using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Models.DealerOnboarding
{
    public class RequiredDocumentDTO : DocumentDTO
    {
        public int Id { get; set; }
        public int DocumentTypeId { get; set; }

        public int? DealerInfoId { get; set; }
        public bool Uploaded { get; set; }

        // Lead source of a client web-portal (DP, MB, OB, CW)
        public string LeadSource { get; set; }
    }
}
