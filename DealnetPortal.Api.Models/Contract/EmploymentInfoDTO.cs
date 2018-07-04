using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration.Employment;

namespace DealnetPortal.Api.Models.Contract
{
    public class EmploymentInfoDTO
    {
        public EmploymentStatus? EmploymentStatus { get; set; }

        public IncomeType? IncomeType { get; set; }

        public EmploymentType? EmploymentType { get; set; }
        public double MonthlyMortgagePayment { get; set; }
        [MaxLength(140)]
        public string JobTitle { get; set; }
        [MaxLength(140)]
        public string CompanyName { get; set; }
        public string CompanyPhone { get; set; }
        public AddressDTO CompanyAddress { get; set; }

        public string AnnualSalary { get; set; }
        public string HourlyRate { get; set; }
        public string LengthOfEmployment { get; set; }
    }
}
