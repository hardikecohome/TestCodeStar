using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Notification
{
    public class MailChimpMember
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public MemberAddress address { get; set; }
        public decimal CreditAmount = 0;
        public string ApplicationStatus { get; set; }
        public string ClosingDateRequired = "Not Required";
        public string EquipmentInfoRequired = "Not Required";
        public string TemporaryPassword { get; set; }
        public string OneTimeLink { get; set; }
        public string DealerLeadAccepted = "Waiting to accept";

    }
    public class MemberAddress
    {
        public string Street { get; set; }
        public string Unit { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country = "CA";
    }
    
}
