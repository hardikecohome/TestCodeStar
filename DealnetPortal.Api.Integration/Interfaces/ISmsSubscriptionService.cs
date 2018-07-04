using System.Threading.Tasks;
using DealnetPortal.Api.Integration.SMSSubscriptionManagement;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface ISmsSubscriptionService
    {
        Task<startSubscriptionResponse> SetStartSubscription(string phone, string reference, string code, string contentReference);
    }
}