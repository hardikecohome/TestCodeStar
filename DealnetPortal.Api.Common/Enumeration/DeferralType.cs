using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DealnetPortal.Api.Common.Attributes;

namespace DealnetPortal.Api.Common.Enumeration
{
    public enum DeferralType
    {
        [Display(Name = "No Deferral")]
        [PersistentDescription("No Deferral")]
        NoDeferral,
        [Display(Name = "2 Month")]
        [PersistentDescription("2 Month")]
        TwoMonth,
        [Display(Name = "3 Month")]
        [PersistentDescription("3 Month")]
        ThreeMonth,
        [Display(Name = "6 Month")]
        [PersistentDescription("6 Month")]
        SixMonth,
        [Display(Name = "9 Month")]
        [PersistentDescription("9 Month")]
        NineMonth,
        [Display(Name = "12 Month")]
        [PersistentDescription("12 Month")]
        TwelveMonth
    }
}
