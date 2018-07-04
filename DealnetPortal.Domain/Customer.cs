using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    public class Customer
    {
        public Customer()
        {
            Locations = new HashSet<Location>();
            Phones = new HashSet<Phone>();
            Emails = new HashSet<Email>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }

        [StringLength(9, MinimumLength = 9)]
        public string Sin { get; set; }
        public string DriverLicenseNumber { get; set; }
        
        public virtual ICollection<Location> Locations { get; set; }

        public virtual ICollection<Phone> Phones { get; set; }

        public virtual ICollection<Email> Emails { get; set; }

        public string AccountId { get; set; }

        public bool? AllowCommunicate { get; set; }

        public bool? ExistingCustomer { get; set; }

        public PreferredContactMethod? PreferredContactMethod { get; set; }

        public string VerificationIdName { get; set; }

        public string DealerInitial { get; set; }

        [MaxLength(50)]
        public string RelationshipToMainBorrower { get; set; }

        public EmploymentInfo EmploymentInfo { get; set; }

        public CustomerCreditReport CreditReport { get; set; }

        public bool? IsDeleted { get; set; }
    }
}
