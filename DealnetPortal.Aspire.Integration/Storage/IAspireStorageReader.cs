using System;
using System.Collections.Generic;
using DealnetPortal.Aspire.Integration.Models.AspireDb;

namespace DealnetPortal.Aspire.Integration.Storage
{
    public interface IAspireStorageReader
    {
        IList<DropDownItem> GetGenericFieldValues();

        IList<GenericSubDealer> GetSubDealersList(string dealerUserName);

        IList<Contract> GetDealerDeals(string dealerUserName);

        Entity GetDealerInfo(string dealerUserName);

        //Contract GetDealById(int transactionId);

        Entity GetCustomerById(string customerId);

        //CustomerDTO FindCustomer(CustomerDTO customer);

        Entity FindCustomer(string firstName, string lastName, DateTime dateOfBirth, string postalCode);

        DealerRoleEntity GetDealerRoleInfo(string dealerUserName);

        string GetDealStatus(string transactionId);

        CreditReport GetCustomerCreditReport(string customerId);
        CreditReport GetCustomerCreditReport(string firstName, string lastName, DateTime dateOfBirth, string postalCode);

        IList<CustomerAgreementShortInfo> SearchCustomerAgreements(string firstName, string lastName, DateTime dateOfBirth);
    }
}
