using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class SubDealerDTO
    {
        public string DealerId { get; set; }
        public string SeqNum { get; set; }
        public string DealerName { get; set; }
        public string SubDealerId { get; set; }
        public string SubDealerName { get; set; }
        public string SubmissionValue { get; set; }
    }
}
