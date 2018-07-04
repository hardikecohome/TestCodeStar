using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration.Dealer;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Models.DealerOnboarding
{
    public class CompanyInfoDTO
    {
        public int Id { get; set; }
        public string FullLegalName { get; set; }
        public string OperatingName { get; set; }

        public string AccountId { get; set; }

        public string Phone { get; set; }
        public string EmailAddress { get; set; }
        public string Website { get; set; }

        public AddressDTO CompanyAddress { get; set; }

        public YearsInBusiness? YearsInBusiness { get; set; }
        public NumberOfPeople? NumberOfInstallers { get; set; }
        public NumberOfPeople? NumberOfSales { get; set; }
        public BusinessType? BusinessType { get; set; }

        public List<string> Provinces { get; set; }
    }
}
