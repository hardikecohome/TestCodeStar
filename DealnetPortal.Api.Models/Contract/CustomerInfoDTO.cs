using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class CustomerInfoDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required]
        public DateTime? DateOfBirth { get; set; }

        public string Sin { get; set; }
        public string DriverLicenseNumber { get; set; }

        public bool? AllowCommunicate { get; set; }

        public PreferredContactMethod? PreferredContactMethod { get; set; }

        public string VerificationIdName { get; set; }

        public string DealerInitial { get; set; }

    }
}
