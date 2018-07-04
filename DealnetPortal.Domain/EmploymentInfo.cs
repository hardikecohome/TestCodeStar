using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration.Employment;

namespace DealnetPortal.Domain
{
    public class EmploymentInfo
    {
        public EmploymentInfo()
        {
            CompanyAddress = new Address();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public EmploymentStatus? EmploymentStatus { get; set; }

        public IncomeType? IncomeType { get; set; }

        public EmploymentType? EmploymentType { get; set; }
        public double MonthlyMortgagePayment { get; set; }
        [MaxLength(140)]
        public string JobTitle { get; set; }
        [MaxLength(140)]
        public string CompanyName { get; set; }
        public string CompanyPhone { get; set; }
        public Address CompanyAddress { get; set; }

        public string AnnualSalary { get; set; }
        public string HourlyRate { get; set; }
        public string LengthOfEmployment { get; set; }

        [Required]
        [ForeignKey(nameof(Id))]
        public Customer Customer { get; set; }
    }
}
