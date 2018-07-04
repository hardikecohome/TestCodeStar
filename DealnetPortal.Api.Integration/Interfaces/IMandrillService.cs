using System.Net.Http;
using System.Threading.Tasks;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Notification;
using DealnetPortal.Api.Models.Notify;
using DealnetPortal.Domain;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface IMandrillService
    {
        Task SendDealerLeadAccepted(Contract contract, DealerDTO dealer, string services);
        Task<HttpResponseMessage> SendEmail(MandrillRequest request);
        Task SendHomeImprovementTypeUpdatedConfirmation(string emailid, string firstName, string lastName, string services);
        Task SendDeclineNotificationConfirmation(string emailid, string firstName, string lastName);
        Task SendProblemsWithSubmittingOnboarding(string errorMsg, int dealerInfoId, string accessKey);
        Task SendDraftLinkMail(string accessKey, string email);
        Task SendSupportRequiredEmail(SupportRequestDTO SupportDetails, string email);
        Task SendDeclineToSignDealerNotification(string dealerEmail, string dealerName, string contractId, string customerName, string customerEmail, string customerPhone, string agreementType, string dealerProvince);
        Task SendDealerCustomerLinkFormSubmittedNotification(CustomerFormDTO customerFormData, CustomerContractInfoDTO contractData, string dealerProvince);
    }
}