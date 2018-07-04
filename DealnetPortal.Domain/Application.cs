using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class Application
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }

        public string LegalName { get; set; }
        public string FinanceProgram { get; set; }
        public string LeadSource { get; set; }
    }
}
