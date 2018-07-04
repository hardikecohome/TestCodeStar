using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration.Dealer;

namespace DealnetPortal.Domain.Dealer
{
    public class CompanyInfo
    {
        public CompanyInfo()
        {
            CompanyAddress = new Address();
            Provinces = new HashSet<CompanyProvince>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string FullLegalName { get; set; }
        public string OperatingName { get; set; }

        public string AccountId { get; set; }

        public string Phone { get; set; }        
        public string EmailAddress { get; set; }
        public string Website { get; set; }

        public Address CompanyAddress { get; set; }
        
        public YearsInBusiness? YearsInBusiness { get; set; }
        public NumberOfPeople? NumberOfInstallers { get; set; }        
        public NumberOfPeople? NumberOfSales { get; set; }        
        public BusinessType? BusinessType { get; set; }
        //public List<string> Provinces { get; set; }
        //[Required]
        //public DealerInfo DealerInfo { get; set; }

        public virtual ICollection<CompanyProvince> Provinces { get; set; } 
    }
}
