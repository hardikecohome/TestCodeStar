using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration.Dealer
{
    public enum YearsInBusiness
    {
        [Display(ResourceType = typeof(Resources.Resources), Name ="LessThanOneYear")]
        LessThanOne,
        [Display(ResourceType = typeof(Resources.Resources), Name = "OneToTwoYears")]
        OneToTwo,
        [Display(ResourceType = typeof(Resources.Resources), Name = "MoreThanTwoYears")]
        MoreThanTwo
    }
}
