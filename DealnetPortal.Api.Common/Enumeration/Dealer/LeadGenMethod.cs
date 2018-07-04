using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration.Dealer
{
    public enum LeadGenMethod
    {
        [Display(ResourceType =typeof(Resources.Resources), Name ="Referrals")]
        Referrals,
        [Display(ResourceType = typeof(Resources.Resources), Name = "LocalAds")]
        LocalAdvertising,
        [Display(ResourceType = typeof(Resources.Resources), Name ="TradeShows")]
        TradeShows
    }
}
