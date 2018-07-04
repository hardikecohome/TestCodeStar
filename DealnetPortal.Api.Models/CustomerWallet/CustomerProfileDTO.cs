using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Models.CustomerWallet
{
    public class CustomerProfileDTO
    {
        public string UserId { get; set; }

        public string AccountId { get; set; }
        public string TransactionId { get; set; }
        public decimal? CreditAmount { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }
        [MaxLength(100)]
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public virtual List<LocationDTO> Locations { get; set; }

        public virtual List<PhoneDTO> Phones { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }
    }
}
