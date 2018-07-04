using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Enumeration
{
    public enum WithdrawalDateType
    {
        [Display(Name = "1st")]
        First,
        [Display(Name = "15th")]
        Fifteenth
    }
}
