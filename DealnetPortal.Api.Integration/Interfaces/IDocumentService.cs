using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Api.Models.Storage;

namespace DealnetPortal.Api.Integration.Interfaces
{
    /// <summary>
    /// Service for processing contract documents: e-signature and PDF-versions of contracts, installation certificates, etc.
    /// </summary>
    public interface IDocumentService
    {        
        /// <summary>
        /// Initiate contract signature process
        /// </summary>
        /// <param name="contractId">Id of a contract</param>
        /// <param name="ownerUserId">dealer Id</param>
        /// <param name="signatureUsers">List of signature users for an use in signature process</param>
        /// <returns>Summary about created signature process</returns>
        Task<Tuple<SignatureSummaryDTO, IList<Alert>>> StartSignatureProcess(int contractId, string ownerUserId, SignatureUser[] signatureUsers);
        /// <summary>
        /// Cancel signature process for a contract
        /// </summary>
        /// <param name="contractId">Id of a contract</param>
        /// <param name="ownerUserId">dealer Id</param>
        /// <returns>Summary about canceled signature process</returns>
        Task<Tuple<SignatureSummaryDTO, IList<Alert>>> CancelSignatureProcess(int contractId, string ownerUserId, string cancelReason = null);
        /// <summary>
        /// Clean all signature information related to a contract
        /// </summary>
        /// <param name="contractId">Id of a contract</param>
        /// <param name="ownerUserId">dealer Id</param>
        void CleanSignatureInfo(int contractId, string ownerUserId);

        Task<IList<Alert>> UpdateSignatureUsers(int contractId, string ownerUserId, SignatureUser[] signatureUsers);
        /// <summary>
        /// Process external signature event notification
        /// </summary>
        /// <param name="notificationMsg">notification message</param>
        /// <returns></returns>
        Task<IList<Alert>> ProcessSignatureEvent(string notificationMsg);

        /// <summary>
        /// Update status of signature from eSignature engine for a contract
        /// </summary>
        /// <param name="contractId">contract Id</param>
        /// <param name="ownerUserId">Id of an owner of contract</param>
        /// <returns></returns>
        Task<IList<Alert>> SyncSignatureStatus(int contractId, string ownerUserId);


        /// <summary>
        /// Check if contract agreement for print (any, PDF or eSignature) available
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="documentTypeId"></param>
        /// <param name="ownerUserId"></param>
        /// <returns></returns>
        Task<Tuple<bool, IList<Alert>>> CheckPrintAgreementAvailable(int contractId, int documentTypeId, string ownerUserId);       
        
        /// <summary>
        /// Get contract agreement for print. Try to use PDF template in the Database, if it not exist, try to use DocuSign template
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="ownerUserId"></param>
        /// <returns></returns>
        Task<Tuple<AgreementDocument, IList<Alert>>> GetPrintAgreement(int contractId, string ownerUserId);/// <summary>
        /// Get Signed contract (document) from eSignature engine. if signed doc not exists or still not completed, returns null
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="ownerUserId"></param>
        /// <returns></returns>
        Task<Tuple<AgreementDocument, IList<Alert>>> GetSignedAgreement(int contractId, string ownerUserId);
        /// <summary>
        /// Get filled installation certificate if PDF-template is available
        /// </summary>
        /// <param name="contractId"></param>
        /// <param name="ownerUserId"></param>
        /// <returns></returns>
        Task<Tuple<AgreementDocument, IList<Alert>>> GetInstallCertificate(int contractId, string ownerUserId);
    }
}
