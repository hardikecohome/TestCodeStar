using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Aspire.Integration.Models.AspireDb
{
    public class CustomerAgreementShortInfo
    {
        public string LeaseNum { get; set; }
        //Transaction Id
        public string TransactionId { get; set; }
        
        public string BookType { get; set; }

        public DateTime BookedPostDate { get; set; }
        public DateTime SignDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public string Term { get; set; }
        public string TypeDesc { get; set; }
        //Loan or Rental
        public string Type { get; set; }
        //Customer Id
        public string CustomerId { get; set; }
        public string EquipType { get; set; }
        public string EquipTypeDesc { get; set; }
        public string EquipActiveFlag { get; set; }        
    }
}
