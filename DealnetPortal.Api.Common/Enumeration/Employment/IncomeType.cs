using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Enumeration.Employment
{
    public enum IncomeType
    {
        [Display(ResourceType = typeof(Resources.Resources), Name = "Annual")]
        AnnualSalaryIncome = 0,
        [Display(ResourceType = typeof(Resources.Resources), Name = "HourlyRate")]
        HourlyRate = 1
    }
}
