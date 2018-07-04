using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration.Dealer
{
    public enum Channel
    {
        [Display(ResourceType =typeof(Resources.Resources), Name ="ConsumerDirect")]
        ConsumerDirect,
        [Display(ResourceType =typeof(Resources.Resources), Name ="Broker")]
        Broker,
        [Display(ResourceType =typeof(Resources.Resources), Name ="Distributor")]
        Distributor,
        [Display(ResourceType =typeof(Resources.Resources), Name ="DoorToDoor")]
        DoorToDoor,
    }
}
