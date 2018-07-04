using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Aspire.Integration.Models.AspireDb
{
    public class Entity
    {
        public string EntityId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNum { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }    
        public string Street { get; set; }

        public string ParentUserName { get; set; }

        public string LeaseSource { get; set; }
    }
}
