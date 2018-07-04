using System;
using System.Threading.Tasks;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.SMSSubscriptionManagement;

namespace DealnetPortal.Api.Integration.Services
{
    public class SmsSubscriptionService : ISmsSubscriptionService
    {
        private readonly subscriptionManagementAPI _smsClient = new subscriptionManagementAPIClient();
        private readonly startSubscriptionDTO subscriber = new startSubscriptionDTO();
       
        public async Task<startSubscriptionResponse> SetStartSubscription(string phone, string reference, string code, string contentReference)
        {
            try
            {
                contentServiceIdDTO content = new contentServiceIdDTO();
                content.reference = contentReference;
                subscriber.phone = phone;
                subscriber.reference = reference;
                subscriber.contentService = content;
                subscriber.affiliateCode = code;

                startSubscriptionResponse response = await _smsClient.startSubscriptionAsync(new startSubscription(subscriber));
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
