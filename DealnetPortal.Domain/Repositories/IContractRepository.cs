using System;
using System.Collections.Generic;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Types;

namespace DealnetPortal.Domain.Repositories
{
    /// <summary>
    /// An interface for Contracts repository in DB
    /// </summary>
    public interface IContractRepository
    {
        /// <summary>
        /// Create a new contract (deal)
        /// </summary>
        /// <param name="contractOwnerId">user Id</param>
        /// <returns>Contract</returns>
        Contract CreateContract(string contractOwnerId);

        /// <summary>
        /// Get user contracts list
        /// </summary>
        /// <param name="ownerUserId">user Id</param>
        /// <returns>List of contracts</returns>
        IList<Contract> GetContracts(string ownerUserId);

        /// <summary>
        /// Get contract offers for a user (dealer)
        /// </summary>
        /// <param name="ownerUserId">user Id</param>
        /// <returns>List of contracts</returns>
        IList<Contract> GetDealerLeads(string userId);

        /// <summary>
        /// Get contract created by an user (dealer)
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <returns>List of contracts</returns>
        IList<Contract> GetContractsCreatedByUser(string userId);

        IList<Contract> GetExpiredContracts(DateTime expiredDate);

        /// <summary>
        /// Get count of customers user contracts list
        /// </summary>
        /// <param name="ownerUserId">user Id</param>
        /// <returns>numder of contracts</returns>
        int GetNewlyCreatedCustomersContractsCount(string ownerUserId);

        /// <summary>
        /// Get list of user contracts with particular ids
        /// </summary>
        /// <param name="ids">List od Ids</param>
        /// <param name="ownerUserId">user Id</param>
        /// <returns>List of contracts</returns>
        IList<Contract> GetContracts(IEnumerable<int> ids, string ownerUserId);

        IList<Contract> GetContractsEquipmentInfo(IEnumerable<int> ids, string ownerUserId);

        Contract FindContractBySignatureId(string signatureTransactionId);

        Customer GetCustomer(int customerId);

        ContractState? GetContractState(int contractId, string contractOwnerId);

        /// <summary>
        /// Delete contract with all linked data
        /// </summary>
        /// <param name="contractOwnerId">contract owner user Id</param>
        /// <param name="contractId">contract Id</param>
        /// <returns>Result of delete</returns>
        bool DeleteContract(string contractOwnerId, int contractId);

        /// <summary>
        /// Clean all data linked with contract, except contract general info
        /// </summary>
        /// <param name="contractOwnerId">contract owner user Id</param>
        /// <param name="contractId">contract Id</param>
        /// <returns>Result of clean</returns>
        bool CleanContract(string contractOwnerId, int contractId);

        /// <summary>
        /// Update contract entity in DB
        /// </summary>
        /// <param name="contract">Contract</param>
        /// <returns>Contract</returns>
        Contract UpdateContract(Contract contract, string contractOwnerId);

        /// <summary>
        /// Update contract data
        /// </summary>
        /// <param name="contractData">Structure with data related to contract</param>
        /// <returns>Result of update</returns>
        Contract UpdateContractData(ContractData contractData, string contractOwnerId);        

        /// <summary>
        /// Update customer
        /// </summary>
        /// <param name="customer">Customer data</param>
        Customer UpdateCustomer(Customer customer);

        /// <summary>
        /// Update customer data
        /// </summary>
        /// <param name="customerId">Customer Id</param>
        /// <param name="customerInfo"></param>
        /// <param name="locations">locations to set</param>
        /// <param name="phones">phones to set</param>
        /// <param name="emails">emails to set</param>
        /// <returns>Is customer updated</returns>
        bool UpdateCustomerData(int customerId, Customer customerInfo, IList<Location> locations, IList<Phone> phones, IList<Email> emails);

        bool UpdateCustomerEmails(int customerId, IList<Email> emails);        

        /// <summary>
        /// Update contract state
        /// </summary>
        /// <param name="contractId">Contract Id</param>
        /// <param name="newState">A new state of contract</param>
        /// <returns>Updated contract record</returns>
        Contract UpdateContractState(int contractId, string contractOwnerId, ContractState newState);

        Contract UpdateContractAspireSubmittedDate(int contractId, string contractOwnerId);

        Contract UpdateContractSigners(int contractId, IList<ContractSigner> signers, string contractOwnerId, bool syncOnly = false);

        /// <summary>
        /// Get contract
        /// </summary>
        /// <param name="contractId">Contract Id</param>
        /// <returns>Contract</returns>
        Contract GetContract(int contractId, string contractOwnerId);

        Contract GetContract(int contractId);

        /// <summary>
        /// Get contract as untracked from DB
        /// </summary>
        /// <param name="contractId">Contract Id</param>
        /// <returns>Contract</returns>
        Contract GetContractAsUntracked(int contractId, string contractOwnerId);

        /// <summary>
        /// Get Equipment Types list
        /// </summary>
        /// <returns>List of Equipment Type</returns>
        IList<EquipmentType> GetEquipmentTypes();

        EquipmentType GetEquipmentTypeInfo(string type);

        /// <summary>
        /// Get Document Types list
        /// </summary>
        /// <returns>List of Equipment Type</returns>
        IList<DocumentType> GetAllDocumentTypes();

        /// <summary>
        /// Get Document Types list
        /// </summary>
        /// <returns>List of Equipment Type</returns>
        IList<DocumentType> GetStateDocumentTypes(string state);

        IList<DocumentType> GetDealerDocumentTypes(string state, string contractOwnerId);

        IDictionary<string, IList<DocumentType>> GetDealerDocumentTypes(string contractOwnerId);

        /// <summary>
        /// Get Document Types specified for contract, that can be defined for province and/or dealer
        /// </summary>
        /// <returns>List of Equipment Type</returns>
        IList<DocumentType> GetContractDocumentTypes(int contractId, string contractOwnerId);

        /// <summary>
        /// Get Province Tax Rate
        /// </summary>
        /// <param name="province">Province abbreviation</param>
        /// <returns>Tax Rate for particular Province</returns>
        ProvinceTaxRate GetProvinceTaxRate(string province);

        /// <summary>
        /// Get Province All Tax Rates
        /// </summary>
        /// <returns>Tax Rates for all provinces</returns>
        IList<ProvinceTaxRate> GetAllProvinceTaxRates();

        /// <summary>
        /// Get VerificationId
        /// </summary>
        /// <param name="VerificationId">Province abbreviation</param>
        /// <returns>VerificationId for particular id</returns>
        VerifiactionId GetVerficationId(int id);

        /// <summary>
        /// Get All Verification Ids
        /// </summary>
        /// <returns>All VerificationIds</returns>
        IList<VerifiactionId> GetAllVerificationIds();

        AspireStatus GetAspireStatus(string status);
        
        PaymentSummary GetContractPaymentsSummary(int contractId, decimal? priceOfEquipment = null);

        Comment TryAddComment(Comment comment, string contractOwnerId);

        /// <summary>
        /// Removes contract comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="contractOwnerId"></param>
        /// <returns>Contract id of removed comment. If removal fails, then null is returned</returns>
        int? RemoveComment(int commentId, string contractOwnerId);

        ContractDocument AddDocumentToContract(int contractId, ContractDocument document, string contractOwnerId);

        bool TryRemoveContractDocument(int documentId, string contractOwnerId);

        IList<ContractDocument> GetContractDocumentsList(int contractId, string contractOwnerId);


        IList<ApplicationUser> GetSubDealers(string dealerId);

        ApplicationUser GetDealer(string dealerId);

        /// <summary>
        /// Update Dealer-SubDealers hierarchy by array of related transactions
        /// </summary>
        /// <param name="transactionIds">array of transactions Ids related to dealer</param>
        /// <param name="ownerUserId"></param>
        /// <returns>number of updated sub-dealers</returns>
        int UpdateSubDealersHierarchyByRelatedTransactions(IEnumerable<string> transactionIds, string ownerUserId);

        /// <summary>
        /// resign contract to another dealer
        /// </summary>
        /// <param name="contractId">contractId</param>
        /// <param name="newContractOwnerId">new dealer id</param>
        /// <returns></returns>
        Contract AssignContract(int contractId, string newContractOwnerId);

        bool IsContractUnassignable(int contractId);

        bool IsClarityProgram(int contractId);

        bool IsBill59Contract(int contractId);

        bool IsMortgageBrokerCustomerExist(string email);
    }
}
