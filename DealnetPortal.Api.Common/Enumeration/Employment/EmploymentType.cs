using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Enumeration.Employment
{
    public enum EmploymentType
    {
        [Display(ResourceType = typeof(Resources.Resources), Name = "FullTime")]
        FullTime = 0,
        [Display(ResourceType = typeof(Resources.Resources), Name = "PartTime")]
        PartTime = 1
    }
}
