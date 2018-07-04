using System.Threading.Tasks;
using DealnetPortal.Aspire.Integration.Models;

namespace DealnetPortal.Aspire.Integration.ServiceAgents
{
    /// <summary>
    /// Service agent for communicate with ASPIRE API
    /// </summary>
    public interface IAspireServiceAgent
    {
        /// <summary>
        /// Call deal upload submission
        /// </summary>
        /// <param name="dealUploadRequest"></param>
        /// <returns></returns>
        Task<DealUploadResponse> DealUploadSubmission(DealUploadRequest dealUploadRequest);

        /// <summary>
        /// Customer information update to Aspire
        /// </summary>
        /// <param name="customerRequest"></param>
        /// <returns></returns>
        Task<DecisionCustomerResponse> CustomerUploadSubmission(CustomerRequest customerRequest);

        /// <summary>
        /// Credit Check Submission
        /// </summary>
        /// <param name="dealUploadRequest"></param>
        /// <returns></returns>
        Task<CreditCheckResponse> CreditCheckSubmission(CreditCheckRequest dealUploadRequest);

        Task<DecisionLoginResponse> LoginSubmission(DealUploadRequest dealUploadRequest);

        Task<DocumentUploadResponse> DocumentUploadSubmission(DocumentUploadRequest docUploadRequest);
    }
}
