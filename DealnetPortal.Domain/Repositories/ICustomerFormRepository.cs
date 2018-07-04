using System.Collections.Generic;

namespace DealnetPortal.Domain.Repositories
{
    public interface ICustomerFormRepository
    {
        CustomerLink GetCustomerLinkSettings(string dealerId);
        CustomerLink GetCustomerLinkSettingsByDealerName(string dealerName);
        CustomerLink GetCustomerLinkSettingsByHashDealerName(string hashDealerName);
        CustomerLink UpdateCustomerLinkLanguages(ICollection<DealerLanguage> enabledLanguages, string hashLink, string dealerId);
        CustomerLink UpdateCustomerLinkServices(ICollection<DealerService> dealerServices, string hashLink, string dealerId);
        Contract AddCustomerContractData(int contractId, string commentsHeader, string selectedService, string customerComment, string dealerId);        
    }
}
