using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Domain;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface IAspireService
    {
        /// <summary>
        /// Check aspire user credentials
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<IList<Alert>> LoginUser(string userName, string password);
        /// <summary>
        /// Prepare request and call Aspire CustomerUpload
        /// </summary>
        /// <param name="contractId">Id of contract</param>
        /// <param name="contractOwnerId">dealer Id</param>
        /// <param name="leadSource">value of lead source of a client app</param>
        /// <returns></returns>
        Task<IList<Alert>> UpdateContractCustomer(int contractId, string contractOwnerId, string leadSource = null);
        Task<IList<Alert>> UpdateContractCustomer(Contract contract, string contractOwnerId, string leadSource = null, bool withSymbolsMapping = false);
        /// <summary>
        /// Send request for credit check and get results
        /// </summary>
        /// <param name="contractId">Id of contract</param>
        /// <param name="contractOwnerId">dealer Id</param>
        /// <returns>Credit check results</returns>
        Task<Tuple<CreditCheckDTO, IList<Alert>>> InitiateCreditCheck(int contractId, string contractOwnerId);        

        /// <summary>
        /// Submit deal with equipment and payment information
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="contractOwnerId"></param>
        /// <param name="leadSource">value of lead source of a client app</param>
        /// <returns></returns>
        Task<IList<Alert>> SubmitDeal(int contractId, string contractOwnerId, string leadSource = null);
        /// <summary>
        /// Submit deal without equipment information - send UDFs only
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="contractOwnerId"></param>
        /// <param name="leadSource">value of lead source of a client app</param>
        /// <returns></returns>
        Task<IList<Alert>> SendDealUDFs(int contractId, string contractOwnerId, string leadSource = null);
        Task<IList<Alert>> SendDealUDFs(Contract contract, string contractOwnerId, string leadSource = null, ContractorDTO contractor = null);
        /// <summary>
        /// Submit dealer's onboarding form data
        /// </summary>
        /// <returns></returns>
        Task<IList<Alert>> SubmitDealerOnboarding(int dealerInfoId, string leadSource = null);        
        /// <summary>
        /// Change status of Aspire Deal
        /// </summary>
        /// <param name="aspireTransactionId">Transaction Id for a deal</param>
        /// <param name="newStatus">new Aspire status</param>
        /// <param name="contractOwnerId">dealer Id</param>
        /// <returns></returns>
        Task<Tuple<string, IList<Alert>>> ChangeDealStatusEx(string aspireTransactionId, string newStatus, string contractOwnerId);
        Task<IList<Alert>> ChangeDealStatus(string aspireTransactionId, string newStatus, string contractOwnerId, string additionalDataToPass = null);
        Task<Tuple<string, IList<Alert>>> ChangeDealStatusByCreditReview(string aspireTransactionId, string newStatus, string contractOwnerId);                
        /// <summary>
        /// Upload file or document to Aspire
        /// </summary>
        /// <param name="contractId">Id of a contract</param>
        /// <param name="document">uploaded document</param>
        /// <param name="contractOwnerId">dealer Id</param>
        /// <returns></returns>
        Task<IList<Alert>> UploadDocument(int contractId, ContractDocumentDTO document, string contractOwnerId);
        Task<IList<Alert>> UploadDocument(string aspireTransactionId, ContractDocumentDTO document, string contractOwnerId);
        Task<IList<Alert>> UploadOnboardingDocument(int dealerInfoId, int requiredDocId, string statusToSend = null);
    }
}
