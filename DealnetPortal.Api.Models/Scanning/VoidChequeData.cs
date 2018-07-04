using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Scanning
{
    public class VoidChequeData
    {
        public string BankNumber { get; set; }
        public string TransitNumber { get; set; }
        public string AccountNumber { get; set; }
    }
}
