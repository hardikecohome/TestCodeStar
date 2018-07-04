using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Controllers
{
    [Authorize]
    [RoutePrefix("api/Contract")]
    public class ContractController : BaseApiController
    {
        private IContractService _contractService { get; set; }
        private ICustomerFormService _customerFormService { get; set; }
        private IRateCardsService _rateCardsService { get; set; }
        private IDocumentService _documentService { get; set; }
        private ICreditCheckService _creditCheckService { get; set; }
        private ICustomerWalletService _customerWalletService { get; set; }

        public ContractController(ILoggingService loggingService, IContractService contractService, ICustomerFormService customerFormService, IRateCardsService rateCardsService,
            IDocumentService documentService, ICreditCheckService creditCheckService, ICustomerWalletService customerWalletService)
            : base(loggingService)
        {
            _contractService = contractService;
            _customerFormService = customerFormService;
            _rateCardsService = rateCardsService;
            _documentService = documentService;
            _creditCheckService = creditCheckService;
            _customerWalletService = customerWalletService;
        }

        // GET: api/Contract
        [HttpGet]
        public IHttpActionResult GetContract()
        {
            try
            {            
                var contracts = _contractService.GetContracts(LoggedInUser.UserId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get contracts for the User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("GetCustomersContractsCount")]
        [HttpGet]
        public IHttpActionResult GetCustomersContractsCount()
        {
            try
            {
                var contracts = _contractService.GetCustomersContractsCount(LoggedInUser.UserId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get number of customers contracts for the User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("GetCompletedContracts")]
        [HttpGet]
        public IHttpActionResult GetCompletedContracts()
        {
            var contracts = _contractService.GetContracts(LoggedInUser.UserId);
            return Ok(contracts.Where(c => c.ContractState >= ContractState.Completed));
        }

        //Get: api/Contract/{contractId}
        [Route("{contractId}")]
        [HttpGet]
        public IHttpActionResult GetContract(int contractId)
        {
            var contract = _contractService.GetContract(contractId, LoggedInUser?.UserId);
            if (contract != null)
            {
                return Ok(contract);
            }

            return NotFound();
        }

        [Route("GetContracts")]
        [HttpPost]
        public IHttpActionResult GetContracts(IEnumerable<int> ids)
        {
            try
            {
                var contracts = _contractService.GetContracts(ids, LoggedInUser?.UserId);
                return Ok(contracts);                
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetDealerLeads")]
        [HttpGet]
        public IHttpActionResult GetDealerLeads()
        {
            try
            {
                var contractsOffers = _contractService.GetDealerLeads(LoggedInUser.UserId);
                return Ok(contractsOffers);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get contracts offers for the User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("CreateContract")]
        [HttpPut]
        public IHttpActionResult CreateContract()
        {
            var alerts = new List<Alert>();
            try
            {
                var contract = _contractService.CreateContract(LoggedInUser?.UserId);
                if (contract == null)
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ContractCreateFailed,
                        Message = $"Failed to create contract for a user [{LoggedInUser?.UserId}]"
                    });
                }
                return Ok(new Tuple<ContractDTO, IList<Alert>>(contract, alerts));
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.ContractCreateFailed,
                    Message = ex.ToString()
                });
            }
            return Ok(new Tuple<ContractDTO, IList<Alert>>(null, alerts));
        }

        [Route("UpdateContractData")]
        [HttpPut]
        public IHttpActionResult UpdateContractData(ContractDataDTO contractData)
        {
            try
            {
                var alerts = _contractService.UpdateContractData(contractData, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{contractId}/NotifyEdit")]
        [HttpPut]
        public IHttpActionResult NotifyContractEdit(int contractId)
        {
            try
            {
                var alerts = _contractService.NotifyContractEdit(contractId, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("InitiateCreditCheck")]
        [HttpPut]
        public IHttpActionResult InitiateCreditCheck(int contractId)
        {
            try
            {
                //TODO: remove after CW review
                //var alerts = _creditCheckService.InitiateCreditCheck(contractId, LoggedInUser?.UserId);
                var alerts = new List<Alert>();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("InitiateDigitalSignature")]
        [HttpPut]
        public async Task<IHttpActionResult> InitiateDigitalSignature(SignatureUsersDTO users)
        {
            try
            {
                var summary = await _documentService.StartSignatureProcess(users.ContractId, LoggedInUser?.UserId, users.Users?.ToArray());
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("UpdateContractSigners")]
        [HttpPut]
        public async Task<IHttpActionResult> UpdateContractSigners(SignatureUsersDTO users)
        {
            try
            {
                var alerts = await _documentService.UpdateSignatureUsers(users.ContractId, LoggedInUser?.UserId, users.Users?.ToArray());
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("CancelDigitalSignature")]
        [HttpPost]
        public async Task<IHttpActionResult> CancelDigitalSignature(int contractId)
        {
            try
            {
                var result = await _documentService.CancelSignatureProcess(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }        

        [Route("AddDocument")]
        [HttpPut]
        public IHttpActionResult AddDocumentToContract(ContractDocumentDTO document)
        {
            try
            {
                var result = _contractService.AddDocumentToContract(document, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("RemoveDocument")]
        [HttpPost]
        public IHttpActionResult RemoveContractDocument(int documentId)
        {
            try
            {
                var result = _contractService.RemoveContractDocument(documentId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("SubmitAllDocumentsUploaded")]
        [HttpPost]
        public async Task<IHttpActionResult> SubmitAllDocumentsUploaded(int contractId)
        {
            try
            {
                var result = await _contractService.SubmitAllDocumentsUploaded(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{contractId}/creditcheck")]
        [HttpGet]
        public IHttpActionResult GetCreditCheckResult(int contractId)
        {
            try
            {
                var result = _creditCheckService.ContractCreditCheck(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{contractId}/DocumentTypes")]
        [HttpGet]
        public IHttpActionResult GetContractDocumentTypes(int contractId)
        {
            try
            {
                var result = _contractService.GetContractDocumentTypes(contractId, LoggedInUser?.UserId);                    
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetContractAgreement")]
        [HttpGet]
        public async Task<IHttpActionResult> GetContractAgreement(int contractId)
        {
            try
            {
                var result = await _documentService.GetPrintAgreement(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetSignedAgreement")]
        [HttpGet]
        public async Task<IHttpActionResult> GetSignedAgreement(int contractId)
        {
            try
            {                
                var result = await _documentService.GetSignedAgreement(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("CheckContractAgreementAvailable")]
        [HttpGet]
        public async Task<IHttpActionResult> CheckContractAgreementAvailable(int contractId)
        {
            try
            {
                var result = await _documentService.CheckPrintAgreementAvailable(contractId, (int) DocumentTemplateType.SignedContract, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetInstallationCertificate")]
        [HttpGet]
        public async Task<IHttpActionResult> GetInstallationCertificate(int contractId)
        {
            try
            {                
                var result = await _documentService.GetInstallCertificate(contractId, LoggedInUser?.UserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("CheckInstallationCertificateAvailable")]
        [HttpGet]
        public async Task<IHttpActionResult> CheckInstallationCertificateAvailable(int contractId)
        {
            try
            {
                var result = await _documentService.CheckPrintAgreementAvailable(contractId, (int)DocumentTemplateType.SignedInstallationCertificate, LoggedInUser?.UserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{summaryType}/ContractsSummary")]
        [HttpGet]
        public IHttpActionResult GetContractsSummary(string summaryType)
        {
            FlowingSummaryType type;
            Enum.TryParse(summaryType, out type);

            var result = _contractService.GetDealsFlowingSummary(LoggedInUser?.UserId, type);
            return Ok(result);
        }        

        [Route("CreateXlsxReport")]
        [HttpPost]
        public IHttpActionResult CreateXlsxReport(Tuple<IEnumerable<int>, int?> reportData)
        {
            try
            {
                var report = _contractService.GetContractsFileReport(reportData.Item1, LoggedInUser.UserId, reportData.Item2);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }        

        [Route("GetCustomer")]
        [HttpGet]
        public IHttpActionResult GetCustomer(int customerId)
        {
            var customer = _contractService.GetCustomer(customerId);
            if (customer != null)
            {
                return Ok(customer);
            }
            return NotFound();
        }

        [Route("UpdateCustomerData")]
        [HttpPut]
        public IHttpActionResult UpdateCustomerData([FromBody]CustomerDataDTO[] customers)
        {
            try
            {
                var alerts = _contractService.UpdateCustomers(customers, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("UpdateInstallationData")]
        [HttpPut]
        public IHttpActionResult UpdateInstallationData(InstallationCertificateDataDTO installationCertificateData)
        {
            try
            {
                var alerts = _contractService.UpdateInstallationData(installationCertificateData, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("SubmitContract")]
        [HttpPost]
        public IHttpActionResult SubmitContract(int contractId)
        {
            try
            {
                var submitResult = _contractService.SubmitContract(contractId, LoggedInUser?.UserId);
                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("AddComment")]
        [HttpPost]
        public IHttpActionResult AddComment(CommentDTO comment)
        {
            try
            {
                var alerts = _contractService.AddComment(comment, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("RemoveComment")]
        [HttpPost]
        public IHttpActionResult RemoveComment(int commentId)
        {
            try
            {
                var alerts = _contractService.RemoveComment(commentId, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        

        [Route("SubmitCustomerServiceRequest")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IHttpActionResult> SubmitCustomerServiceRequest(CustomerServiceRequestDTO customerServiceRequest)
        {
            try
            {
                var submitResult = await _customerFormService.CustomerServiceRequest(customerServiceRequest).ConfigureAwait(false);
                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("RemoveContract")]
        [HttpPost]
        public IHttpActionResult RemoveContract(int contractId)
        {
            try
            {
                var result = _contractService.RemoveContract(contractId, LoggedInUser?.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("AssignContract")]
        [HttpPost]
        public async Task<IHttpActionResult> AssignContract(int contractId)
        {
            try
            {
                var result = await _contractService.AssignContract(contractId, LoggedInUser?.UserId).ConfigureAwait(false);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetDealerTier")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetDealerTier()
        {
            try
            {
                var submitResult = _rateCardsService.GetRateCardsByDealerId(LoggedInUser?.UserId);

                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetDealerTier")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetDealerTier(int contractId)
        {
            try
            {
                var submitResult = _rateCardsService.GetRateCardsForContract(contractId, LoggedInUser?.UserId);

                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("CheckCustomerExisting")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IHttpActionResult> CheckCustomerExistingAsync([FromUri]string email)
        {
            try
            {
                var result = await _customerWalletService.CheckCustomerExisting(email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
