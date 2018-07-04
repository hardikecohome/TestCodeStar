using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerDTO
    {        
        public int Id { get; set; }
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }

        public string Sin { get; set; }
        public string DriverLicenseNumber { get; set; }

        public bool? AllowCommunicate { get; set; }

        public PreferredContactMethod? PreferredContactMethod { get; set; }

        public bool? IsHomeOwner { get; set; }
        
        public bool? IsInitialCustomer { get; set; }       

        public List<LocationDTO> Locations { get; set; }

        public List<PhoneDTO> Phones { get; set; }

        public List<EmailDTO> Emails { get; set; }

        public string AccountId { get; set; }

        public string VerificationIdName { get; set; }

        public string DealerInitial { get; set; }

        public EmploymentInfoDTO EmploymentInfo { get; set; }

        public CustomerCreditReportDTO CreditReport { get; set; }

        public string RelationshipToMainBorrower { get; set; }
    }
}
