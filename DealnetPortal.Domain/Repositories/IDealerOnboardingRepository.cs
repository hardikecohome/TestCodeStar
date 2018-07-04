using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Domain.Dealer;

namespace DealnetPortal.Domain.Repositories
{
    public interface IDealerOnboardingRepository
    {
        DealerInfo GetDealerInfoById(int id);
        DealerInfo GetDealerInfoByAccessKey(string accessKey);

        DealerInfo AddOrUpdateDealerInfo(DealerInfo dealerInfo);
        bool DeleteDealerInfo(int dealerInfoId);
        bool DeleteDealerInfo(string accessCode);

        RequiredDocument AddDocumentToDealer(int dealerInfoId, RequiredDocument document);
        bool DeleteDocumentFromDealer(int documentId);
        RequiredDocument SetDocumentStatus(int documentId, DocumentStatus newStatus);
        RequiredDocument AddDocumentToDealer(string accessCode, RequiredDocument document);
    }
}
