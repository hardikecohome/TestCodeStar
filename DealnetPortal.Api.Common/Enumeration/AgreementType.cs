using System;
using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration
{
    [Flags]
    public enum AgreementType
    {
        [Display(ResourceType = typeof (Resources.Resources), Name = "LoanApplication")]
        LoanApplication = 0,
        [Display(ResourceType = typeof (Resources.Resources), Name = "LeaseApplicationHwt")]
        RentalApplicationHwt = 1,
        [Display(ResourceType = typeof (Resources.Resources), Name = "LeaseApplication")]
        RentalApplication = 2,
        [Display(ResourceType = typeof(Resources.Resources), Name = "LeaseApplication")]
        Rental = RentalApplicationHwt | RentalApplication,
    }
}
