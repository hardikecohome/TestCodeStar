using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration.Dealer
{
    public enum NumberOfPeople
    {
        [Display(Name = "0")]
        Zero,
        [Display(Name = "1-2")]
        OneToTwo,
        [Display(Name = "3-5")]
        ThreeToFive,
        [Display(Name = "6-9")]
        SixToNine,
        [Display(ResourceType = typeof(Resources.Resources), Name = "TenOrMore")]
        MoreThanTen
    }
}
