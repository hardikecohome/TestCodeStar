using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class ProvinceTaxRateDTO
    {
        public int Id { get; set; }
        public string Province { get; set; }
        public double Rate { get; set; }
        public string Description { get; set; }
    }
}
