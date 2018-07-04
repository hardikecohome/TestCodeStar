using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Enumeration.Dealer;
using DealnetPortal.Api.Common.Enumeration.Employment;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Helpers;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Aspire.Integration.Constants;
using DealnetPortal.Aspire.Integration.Models;
using DealnetPortal.Aspire.Integration.ServiceAgents;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using Unity.Interception.Utilities;
using Address = DealnetPortal.Aspire.Integration.Models.Address;
using Application = DealnetPortal.Aspire.Integration.Models.Application;

namespace DealnetPortal.Api.Integration.Services
{
    public class AspireService : IAspireService
    {
        private readonly IAspireServiceAgent _aspireServiceAgent;
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly ILoggingService _loggingService;
        private readonly IContractRepository _contractRepository;
        private readonly IDealerOnboardingRepository _dealerOnboardingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;
        private readonly IAppConfiguration _configuration;
        private readonly TimeSpan _aspireRequestTimeout;
        private readonly IRateCardsRepository _rateCardsRepository;
        //Aspire codes
        private const string CodeSuccess = "T000";
        //symbols excluded from document names for upload
        private string DocumentNameReplacedSymbols = " -+=#@$%^!~&;:'`(){}.,|\"";
        private const string BlankValue = "-";

        public AspireService(IAspireServiceAgent aspireServiceAgent, IContractRepository contractRepository, 
            IDealerOnboardingRepository dealerOnboardingRepository,
            IUnitOfWork unitOfWork, IAspireStorageReader aspireStorageReader, IUsersService usersService,
            ILoggingService loggingService, IAppConfiguration configuration, IRateCardsRepository rateCardsRepository)
        {
            _aspireServiceAgent = aspireServiceAgent;
            _aspireStorageReader = aspireStorageReader;
            _contractRepository = contractRepository;
            _dealerOnboardingRepository = dealerOnboardingRepository;
            _loggingService = loggingService;
            _unitOfWork = unitOfWork;
            _usersService = usersService;
            _configuration = configuration;
            _aspireRequestTimeout = TimeSpan.FromSeconds(90);
            _rateCardsRepository = rateCardsRepository;
        }

        public async Task<IList<Alert>> UpdateContractCustomer(int contractId, string contractOwnerId, string leadSource = null)
        {
            var alerts = new List<Alert>();
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);

            if (contract != null)
            {
                var result = await UpdateContractCustomer(contract, contractOwnerId, leadSource);
                if (result?.Any() == true)
                {
                    alerts.AddRange(result);
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contractId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contractId}");
            }            
            return alerts;
        }

        public async Task<IList<Alert>> UpdateContractCustomer(Contract contract, string contractOwnerId, string leadSource = null, bool withSymbolsMaping = false)
        {
            var alerts = new List<Alert>();

            CustomerRequest request = new CustomerRequest();

            var userResult = GetAspireUser(contractOwnerId);
            if (userResult.Item2.Any())
            {
                alerts.AddRange(userResult.Item2);
            }
            if (alerts.All(a => a.Type != AlertType.Error))
            {
                // Prepare request
                request.Header = userResult.Item1;
                request.Payload = new Payload
                {
                    Lease = new Lease()
                    {
                        Application = new Application()
                        {
                            TransactionId = contract?.Details?.TransactionId
                        },
                        Accounts = GetCustomersInfo(contract, leadSource, withSymbolsMaping)
                    }
                };
                // Send request to Aspire
                var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.CustomerUploadSubmission, request, 
                    (r,c) => AnalyzeResponse(r,c), contract).ConfigureAwait(false);
                if (sendResult?.Any() == true)
                {
                    alerts.AddRange(sendResult);
                }                
            }

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Customers for contract [{contract.Id}] uploaded to Aspire successfully with transaction Id [{contract?.Details.TransactionId}]");
            }

            return alerts;
        }

        public async Task<Tuple<CreditCheckDTO, IList<Alert>>> InitiateCreditCheck(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            CreditCheckDTO creditCheckResult = new CreditCheckDTO();

            if (contract != null)
            {
                if (string.IsNullOrEmpty(contract.Details?.TransactionId))
                {
                    _loggingService.LogWarning(
                        $"Aspire transaction wasn't created for contract {contractId} before credit check. Try to create Aspire transaction");
                    //try to call Customer Update for create aspire transaction
                    await UpdateContractCustomer(contractId, contractOwnerId).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(contract.Details?.TransactionId))
                {
                    CreditCheckRequest request = new CreditCheckRequest();

                    var userResult = GetAspireUser(contractOwnerId);
                    if (userResult.Item2.Any())
                    {
                        alerts.AddRange(userResult.Item2);
                    }
                    if (alerts.All(a => a.Type != AlertType.Error))
                    {
                        request.Header = userResult.Item1;

                        request.Payload = new Payload()
                        {
                            TransactionId = contract.Details.TransactionId
                        };

                        Func<DealUploadResponse, object, bool> CreditCheckAnalyze = (cResponse, cResult) =>
                        {
                            bool succeded = false;
                            var ccresult = cResult as CreditCheckDTO;
                            if (ccresult != null)
                            {
                                var creditCheck = GetCreditCheckResult(cResponse);
                                if (creditCheck != null)
                                {
                                    ccresult.CreditAmount = creditCheck.CreditAmount;
                                    ccresult.CreditCheckState = creditCheck.CreditCheckState;
                                    ccresult.ScorecardPoints = creditCheck.ScorecardPoints;
                                    ccresult.ContractId = contractId;
                                }
                                succeded = true;
                            }
                            return succeded;
                        };

                        // Send request to Aspire
                        var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.CreditCheckSubmission,
                            request, (r,c) => AnalyzeResponse(r,c), contract,
                            CreditCheckAnalyze, creditCheckResult).ConfigureAwait(false);
                        if (sendResult?.Any() == true)
                        {
                            alerts.AddRange(sendResult);
                        }
                    }
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contractId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contractId}");
            }

            alerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                _loggingService.LogError($"Aspire issue: {a.Header} {a.Message}"));

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Aspire credit check for contract [{contractId}] with transaction Id [{contract?.Details?.TransactionId}] initiated successfully");
            }

            return new Tuple<CreditCheckDTO, IList<Alert>>(creditCheckResult, alerts);
        }

        public async Task<IList<Alert>> SubmitDeal(int contractId, string contractOwnerId, string leadSource = null)
        {
            var alerts = new List<Alert>();

            var contract = _contractRepository.GetContract(contractId, contractOwnerId);

            if (contract != null)
            {
                DealUploadRequest request = new DealUploadRequest();

                var userResult = GetAspireUser(contractOwnerId);
                if (userResult.Item2.Any())
                {
                    alerts.AddRange(userResult.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {                   
                    Func<DealUploadResponse, object, bool> equipmentAnalyze =
                        (cResponse, cEquipments) =>
                        {
                            bool succeded = false;
                            var ceqList = (cEquipments as IList<NewEquipment>) ?? contract.Equipment?.NewEquipment.Where(e => e.IsDeleted != true);
                            if (ceqList != null)
                            {
                                cResponse?.Payload?.Asset?.ForEach(asset =>
                                {
                                    if (asset != null)
                                    {
                                        var eqCollection = ceqList;
                                        var aEq = eqCollection?.FirstOrDefault(eq => GetEquipmentDescription(eq) == asset.Name);                                            
                                        if (aEq != null)
                                        {
                                            aEq.AssetNumber = asset.Number;
                                            succeded = true;
                                            _loggingService.LogInfo(
                                                $"Aspire asset number {asset.Number} assigned for equipment for contract {contract.Id}");
                                        }
                                    }
                                });                                
                            }
                            return succeded;
                        };

                    _loggingService.LogInfo($"Submitting deal [{contract.Id}] to Aspire, TransactionId: {contract.Details?.TransactionId}");                    
                    Application application = GetContractApplication(contract, null, null, leadSource);

                    request.Header = new RequestHeader()
                    {
                        From = new From()
                        {
                            AccountNumber = userResult.Item1.UserId,
                            Password = userResult.Item1.Password
                        }
                    };
                    request.Payload = new Payload()
                    {
                        Lease = new Lease()
                        {
                            Application = application
                        }
                    };                   

                    var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.DealUploadSubmission,
                        request, (r, c) => AnalyzeResponse(r, c), contract,
                        equipmentAnalyze).ConfigureAwait(false);
                    if (sendResult?.Any() == true)
                    {
                        alerts.AddRange(sendResult);
                    }
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contractId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contractId}");
            }

            alerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                _loggingService.LogError($"Aspire issue during DealSubmission for transactionId {contract?.Details?.TransactionId}: {a.Header} {a.Message}"));

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Contract [{contractId}] submitted to Aspire successfully with transaction Id [{contract?.Details.TransactionId}]");
            }       

            return alerts;
        }

        public async Task<IList<Alert>> SendDealUDFs(int contractId, string contractOwnerId, string leadSource = null)
        {
            var alerts = new List<Alert>();

            var contract = _contractRepository.GetContract(contractId, contractOwnerId);

            if (contract != null)
            {
                var result = await SendDealUDFs(contract, contractOwnerId, leadSource);
                if (result?.Any() == true)
                {
                    alerts.AddRange(result);
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contractId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contractId}");
            }            

            return alerts;
        }

        public async Task<IList<Alert>> SendDealUDFs(Contract contract, string contractOwnerId, string leadSource = null, ContractorDTO contractor = null)
        {
            var alerts = new List<Alert>();

            DealUploadRequest request = new DealUploadRequest();

            var userResult = GetAspireUser(contractOwnerId);
            if (userResult.Item2.Any())
            {
                alerts.AddRange(userResult.Item2);
            }
            if (alerts.All(a => a.Type != AlertType.Error))
            {
                request.Header = new RequestHeader()
                {
                    From = new From()
                    {
                        AccountNumber = userResult.Item1.UserId,
                        Password = userResult.Item1.Password
                    }
                };
                request.Payload = new Payload()
                {
                    Lease = new Lease()
                    {
                        Application = GetSimpleContractApplication(contract, leadSource, contractor)
                    }
                };

                _loggingService.LogInfo($"Sending deal [{contract.Id}] UDFs to Aspire TransactionId: {request.Payload.Lease.Application.TransactionId}");

                var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.DealUploadSubmission,
                    request, (r,c) => AnalyzeResponse(r,c), contract).ConfigureAwait(false);
                if (sendResult?.Any() == true)
                {
                    alerts.AddRange(sendResult);
                }
            }

            alerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                _loggingService.LogError($"Aspire issue during DealSubmission for transactionId {contract?.Details?.TransactionId}: {a.Header} {a.Message}"));

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Contract [{contract.Id}] submitted to Aspire successfully with transaction Id [{contract?.Details.TransactionId}]");
            }

            return alerts;
        }

        public async Task<IList<Alert>> UploadDocument(int contractId, ContractDocumentDTO document,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();

            var contract = _contractRepository.GetContract(contractId, contractOwnerId);

            if (contract != null && document?.DocumentBytes != null)
            {                
                var docTypeId = document.DocumentTypeId;
                var docTypes = _contractRepository.GetAllDocumentTypes();

                var docType = docTypes?.FirstOrDefault(t => t.Id == docTypeId);
                if (!string.IsNullOrEmpty(docType?.Prefix))
                {
                    if (string.IsNullOrEmpty(document.DocumentName) || !document.DocumentName.StartsWith(docType.Prefix))
                    {
                        document.DocumentName = docType.Prefix + document.DocumentName;
                    }
                }

                var request = new DocumentUploadRequest();

                var userResult = GetAspireUser(contractOwnerId);
                if (userResult.Item2.Any())
                {
                    alerts.AddRange(userResult.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    request.Header = userResult.Item1;

                    try
                    {                    
                        var payload = new DocumentUploadPayload()
                        {
                            TransactionId = contract.Details.TransactionId,                            
                            
                            Status = _configuration.GetSetting(WebConfigKeys.DOCUMENT_UPLOAD_STATUS_CONFIG_KEY)
                        };

                        var uploadName = Regex.Replace(Path.GetFileNameWithoutExtension(document.DocumentName).Replace('[', '_').Replace(']', '_'),
                            $"[{DocumentNameReplacedSymbols}]", "_");

                        var extn = "";
                        if (!String.IsNullOrWhiteSpace(Path.GetExtension(document.DocumentName)))
                        {
                            extn = Path.GetExtension(document.DocumentName)?.Substring(1);
                        }
                        payload.Documents = new List<Document>()
                        {
                            new Document()
                            {
                                Name = uploadName,
                                Data = Convert.ToBase64String(document.DocumentBytes),
                                Ext = extn
                            }
                        };
                        request.Payload = payload;

                        var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.DocumentUploadSubmission,
                            request, (r,c) => AnalyzeResponse(r,c), contract).ConfigureAwait(false);
                        if (sendResult?.Any() == true)
                        {
                            alerts.AddRange(sendResult);
                        }                        
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert()
                        {
                            Code = ErrorCodes.AspireConnectionFailed,
                            Header = "Can't upload document",
                            Message = ex.ToString(),
                            Type = AlertType.Error
                        });
                        _loggingService.LogError($"Can't upload document to Aspire for contract {contractId}", ex);
                    }
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contractId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contractId}");
            }

            return alerts;
        }

        public async Task<IList<Alert>> UploadDocument(string aspireTransactionId, ContractDocumentDTO document,
            string contractOwnerId)
        {
            if (aspireTransactionId == null)
            {
                throw new ArgumentNullException(nameof(aspireTransactionId));
            }
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            if (document.DocumentBytes == null)
            {
                throw new ArgumentNullException(nameof(document.DocumentBytes));
            }

            var alerts = new List<Alert>();

            if (!string.IsNullOrEmpty(aspireTransactionId) && document.DocumentBytes != null)
            {
                var docTypeId = document.DocumentTypeId;
                var docTypes = _contractRepository.GetAllDocumentTypes();

                var docType = docTypes?.FirstOrDefault(t => t.Id == docTypeId);
                if (!string.IsNullOrEmpty(docType?.Prefix))
                {
                    if (string.IsNullOrEmpty(document.DocumentName) || !document.DocumentName.StartsWith(docType.Prefix))
                    {
                        document.DocumentName = docType.Prefix + document.DocumentName;
                    }
                }

                var request = new DocumentUploadRequest();

                var userResult = GetAspireUser(contractOwnerId);
                if (userResult.Item2.Any())
                {
                    alerts.AddRange(userResult.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    request.Header = userResult.Item1;

                    try
                    {
                        var payload = new DocumentUploadPayload()
                        {
                            TransactionId = aspireTransactionId,
                            Status = _configuration.GetSetting(WebConfigKeys.DOCUMENT_UPLOAD_STATUS_CONFIG_KEY)
                        };

                        var uploadName = Regex.Replace(Path.GetFileNameWithoutExtension(document.DocumentName).Replace('[', '_').Replace(']', '_'),
                            $"[{DocumentNameReplacedSymbols}]", "_");

                        payload.Documents = new List<Document>()
                        {
                            new Document()
                            {
                                Name = uploadName,
                                Data = Convert.ToBase64String(document.DocumentBytes),
                                Ext = Path.GetExtension(document.DocumentName)?.Substring(1)
                            }
                        };

                        request.Payload = payload;

                        var docUploadResponse = await _aspireServiceAgent.DocumentUploadSubmission(request).ConfigureAwait(false);
                        if (docUploadResponse?.Header == null || docUploadResponse.Header.Code != CodeSuccess || !string.IsNullOrEmpty(docUploadResponse.Header.ErrorMsg))
                        {
                            alerts.Add(new Alert()
                            {
                                Header = docUploadResponse?.Header?.Status,
                                Message = docUploadResponse?.Header?.Message ?? docUploadResponse?.Header?.ErrorMsg,
                                Type = AlertType.Error
                            });
                        }                       

                        if (alerts.All(a => a.Type != AlertType.Error))
                        {
                            _loggingService.LogInfo($"Document {document.DocumentName} to Aspire for contract with transactionId {aspireTransactionId} was uploaded successfully");
                        }
                        else
                        {
                            alerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                                _loggingService.LogError($"Aspire issue during upload Document {document.DocumentName} to Aspire for contract with transactionId {aspireTransactionId}: {a.Header} {a.Message}"));
                        }
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert()
                        {
                            Code = ErrorCodes.AspireConnectionFailed,
                            Header = "Can't upload document",
                            Message = ex.ToString(),
                            Type = AlertType.Error
                        });
                        _loggingService.LogError($"Can't upload document to Aspire for transaction {aspireTransactionId}", ex);
                    }
                }
            }            

            return alerts;
        }

        public async Task<IList<Alert>> UploadOnboardingDocument(int dealerInfoId, int requiredDocId, string statusToSend = null)
        {
            var alerts = new List<Alert>();

            var dealerInfo = _dealerOnboardingRepository.GetDealerInfoById(dealerInfoId);
            var document = dealerInfo?.RequiredDocuments.First(d => d.Id == requiredDocId);
            if (dealerInfo != null && document != null && document.Uploaded != true)
            {
                var docTypeId = document.DocumentTypeId;
                var docTypes = _contractRepository.GetAllDocumentTypes();

                var docType = docTypes?.FirstOrDefault(t => t.Id == docTypeId);
                if (!string.IsNullOrEmpty(docType?.Prefix))
                {
                    if (string.IsNullOrEmpty(document.DocumentName) || !document.DocumentName.StartsWith(docType.Prefix))
                    {
                        document.DocumentName = docType.Prefix + document.DocumentName;
                    }
                }

                var request = new DocumentUploadRequest();
                var userResult = GetAspireUser(dealerInfo.ParentSalesRepId);
                if (userResult.Item2.Any())
                {
                    alerts.AddRange(userResult.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    request.Header = userResult.Item1;
                    try
                    {
                        var payload = new DocumentUploadPayload()
                        {
                            TransactionId = string.IsNullOrEmpty(dealerInfo.TransactionId) ? null : dealerInfo.TransactionId,
                            Status = statusToSend ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY)
                        };

                        var uploadName = Regex.Replace(Path.GetFileNameWithoutExtension(document.DocumentName).Replace('[', '_').Replace(']', '_'),
                            $"[{DocumentNameReplacedSymbols}]", "_");

                        payload.Documents = new List<Document>()
                        {
                            new Document()
                            {
                                Name = uploadName,
                                Data = Convert.ToBase64String(document.DocumentBytes),
                                Ext = Path.GetExtension(document.DocumentName)?.Substring(1)
                            }
                        };

                        request.Payload = payload;

                        _loggingService.LogInfo($"Uploading document {document.DocumentName} for onboarding form");
                        var sendResults = await DoAspireRequestWithAnalyze(_aspireServiceAgent.DocumentUploadSubmission,
                            request, AnalyzeDealerUploadResponse, dealerInfo,
                            (r, o) =>
                            {
                                document.Uploaded = true;
                                document.UploadDate = DateTime.UtcNow;
                                _loggingService.LogInfo(
                                    $"Document {document.DocumentName} for dealer onboarding form {dealerInfoId} was uploaded to Aspire successfully");
                                return true;
                            }).ConfigureAwait(false);

                        if (sendResults?.Any() == true)
                        {
                            alerts.AddRange(sendResults);
                        }
                        else
                        {
                            sendResults?.Where(a => a.Type == AlertType.Error).ForEach(a =>
                                    _loggingService.LogError($"Aspire issue during upload Document {document.DocumentName} for dealer onboarding form {dealerInfoId}: {a.Header} {a.Message}"));
                        }                        
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert()
                        {
                            Code = ErrorCodes.AspireConnectionFailed,
                            Header = "Can't upload document",
                            Message = ex.ToString(),
                            Type = AlertType.Error
                        });
                        _loggingService.LogError($"Can't upload document to Aspire for dealer onboarding form {dealerInfoId}", ex);
                    }
                }                
            }
            else
            {
                if (document?.Uploaded == true)
                {
                    alerts.Add(new Alert()
                    {                        
                        Header = "Document was uploaded already",
                        Message = "Document was uploaded already",
                        Type = AlertType.Warning
                    });
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Code = ErrorCodes.CantGetContractFromDb,
                        Header = "Can't get dealer onboadring form",
                        Message = $"Can't get dealer onboadring form id {dealerInfoId}",
                        Type = AlertType.Error
                    });
                    _loggingService.LogError($"Can't get dealer onboadring form id {dealerInfoId}");
                }                
            }

            return alerts;
        }        

        public async Task<Tuple<string, IList<Alert>>> ChangeDealStatusEx(string aspireTransactionId, string newStatus,
            string contractOwnerId)
        {
            var tryChangeByCreditReview =
                await ChangeDealStatusByCreditReview(aspireTransactionId, newStatus, contractOwnerId);
            if (tryChangeByCreditReview?.Item2?.Any(a => a.Type == AlertType.Error) == true)
            {
                //sometimes we got an error with Credit Review
                var tryChangeByDocUpload = await ChangeDealStatus(aspireTransactionId, newStatus, contractOwnerId);
                string status = tryChangeByDocUpload?.All(e => e.Type != AlertType.Error) == true ? newStatus : null;
                return new Tuple<string, IList<Alert>>(status, tryChangeByDocUpload);
            }
            else
            {
                return tryChangeByCreditReview;
            }
        }

        public async Task<IList<Alert>> ChangeDealStatus(string aspireTransactionId, string newStatus, string contractOwnerId, string additionalDataToPass = null)
        {
            var alerts = new List<Alert>();            

            var request = new DocumentUploadRequest();

            var userResult = GetAspireUser(contractOwnerId);
            if (userResult.Item2.Any())
            {
                alerts.AddRange(userResult.Item2);
            }
            if (string.IsNullOrEmpty(newStatus))
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Cannot change deal status",
                    Message = "newStatus cannot be null"
                });
            }
            if (alerts.All(a => a.Type != AlertType.Error))
            {
                request.Header = userResult.Item1;

                try
                {
                    var payload = new DocumentUploadPayload()
                    {
                        TransactionId = aspireTransactionId,
                        Status = newStatus
                    };
                    
                    var submitStrBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(additionalDataToPass ?? newStatus));

                    payload.Documents = new List<Document>()
                    {
                        new Document()
                        {
                            Name = newStatus,
                            Data = submitStrBase64,
                            Ext = "txt"
                        }
                    };
                    request.Payload = payload;
                    var docUploadResponse = await _aspireServiceAgent.DocumentUploadSubmission(request).ConfigureAwait(false);
                    if(docUploadResponse?.Header == null || docUploadResponse.Header.Code != CodeSuccess || !string.IsNullOrEmpty(docUploadResponse.Header.ErrorMsg))
                    {
                        alerts.Add(new Alert()
                        {
                            Header = docUploadResponse?.Header?.Status,
                            Message = docUploadResponse?.Header?.Message ?? docUploadResponse?.Header?.ErrorMsg,
                            Type = AlertType.Error
                        });
                    }                    
                    if (alerts.All(a => a.Type != AlertType.Error))
                    {
                        _loggingService.LogInfo($"Aspire state for transaction {aspireTransactionId} was updated successfully");
                    }
                }
                catch (Exception ex)
                {
                    alerts.Add(new Alert()
                    {
                        Code = ErrorCodes.AspireConnectionFailed,
                        Header = $"Can't update state for transaction {aspireTransactionId}",
                        Message = ex.ToString(),
                        Type = AlertType.Error
                    });
                    _loggingService.LogError($"Can't update state for transaction {aspireTransactionId}", ex);
                }
            }

            return alerts;
        }

        public async Task<Tuple<string, IList<Alert>>> ChangeDealStatusByCreditReview(string aspireTransactionId, string newStatus,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();
            string contractStatus = null;
            var request = new CreditCheckRequest();

            var userResult = GetAspireUser(contractOwnerId);
            if (userResult.Item2.Any())
            {
                alerts.AddRange(userResult.Item2);
            }            
            if (alerts.All(a => a.Type != AlertType.Error))
            {
                request.Header = userResult.Item1;

                try
                {
                    request.Payload = new Payload()
                    {
                        TransactionId = aspireTransactionId,
                        ContractStatus = newStatus
                    };
                    
                    var response = await _aspireServiceAgent.CreditCheckSubmission(request).ConfigureAwait(false);
                    if (response?.Header == null || response.Header.Code != CodeSuccess || !string.IsNullOrEmpty(response.Header.ErrorMsg))
                    {
                        alerts.Add(new Alert()
                        {
                            Header = response?.Header?.Status,
                            Message = response?.Header?.Message ?? response?.Header?.ErrorMsg,
                            Type = AlertType.Error
                        });
                    }
                    if (!string.IsNullOrEmpty(response?.Payload?.ContractStatus))
                    {
                        contractStatus = response.Payload.ContractStatus;
                    }

                    if (alerts.All(a => a.Type != AlertType.Error))
                    {
                        _loggingService.LogInfo($"Aspire state for transaction {aspireTransactionId} was updated successfully");
                    }
                }
                catch (Exception ex)
                {
                    alerts.Add(new Alert()
                    {
                        Code = ErrorCodes.AspireConnectionFailed,
                        Header = $"Can't update state for transaction {aspireTransactionId}",
                        Message = ex.ToString(),
                        Type = AlertType.Error
                    });
                    _loggingService.LogError($"Can't update state for transaction {aspireTransactionId}", ex);
                }
            }

            return new Tuple<string, IList<Alert>>(contractStatus, alerts);
        }
        
        public async Task<IList<Alert>> SubmitDealerOnboarding(int dealerInfoId, string leadSource = null)
        {
            var alerts = new List<Alert>();

            var dealerInfo = _dealerOnboardingRepository.GetDealerInfoById(dealerInfoId);

            if (dealerInfo != null)
            {                
                CustomerRequest request = new CustomerRequest();

                var userResult = GetAspireUser(dealerInfo.ParentSalesRepId);
                if (userResult.Item2.Any())
                {
                    alerts.AddRange(userResult.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    request.Header = userResult.Item1;
                    request.Payload = new Payload
                    {
                        Lease = new Lease()
                        {
                            Application = new Application()
                            {
                                TransactionId = dealerInfo.TransactionId
                            },
                            Accounts = GetDealerOnboardingAccounts(dealerInfo, leadSource)
                        }
                    };
                    _loggingService.LogInfo($"Uploading onboarding form to Aspire. Dealer: {dealerInfo.ParentSalesRepId}, TransactionId: {dealerInfo.TransactionId}");
                    var sendResult = await DoAspireRequestWithAnalyze(_aspireServiceAgent.CustomerUploadSubmission,
                        request, AnalyzeDealerUploadResponse, dealerInfo).ConfigureAwait(false);
                    if (sendResult.Any())
                    {
                        alerts.AddRange(sendResult);
                    }                    
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Can't get dealer onboarding info",
                    Message = $"Can't get dealer onboarding info with id {dealerInfoId}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get dealer onboarding info with id {dealerInfoId}");
            }

            alerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                _loggingService.LogError($"Aspire issue during upload onboarding form [{dealerInfoId}] with TransactionId: {dealerInfo?.TransactionId}: {a.Header} {a.Message}"));

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Dealer onboarding form [{dealerInfoId}] submitted to Aspire successfully with transaction Id [{dealerInfo?.TransactionId}]");
            }

            return alerts;
        }

        public async Task<IList<Alert>> LoginUser(string userName, string password)
        {
            var alerts = new List<Alert>();

            DealUploadRequest request = new DealUploadRequest()
            {
                Header = new RequestHeader()
                {
                    UserId = userName,
                    Password = password
                }
            };
            try
            {
                var response = await _aspireServiceAgent.LoginSubmission(request);
                if (response?.Header == null || response.Header.Code != CodeSuccess || !string.IsNullOrEmpty(response.Header.ErrorMsg))
                {
                    alerts.Add(new Alert()
                    {
                        Header = response?.Header?.Code,
                        Message = response?.Header?.Message ?? response?.Header?.ErrorMsg,
                        Type = AlertType.Error
                    });
                }                
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.AspireConnectionFailed,
                    Header = ErrorConstants.AspireConnectionFailed,
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });
                _loggingService.LogError("Failed to communicate with Aspire", ex);
            }
            return alerts;
        }

        #region private      

        private async Task<IList<Alert>> DoAspireRequestWithAnalyze<T1, T2, T3>(Func<T1, Task<T2>> aspireRequest, T1 request,
            Func<T2, T3, Tuple<bool, IList<Alert>>> analyzeResponse, T3 analyzeInput, 
            Func<T2, object, bool> postAnalyze = null, object postAnalyzeInput = null)
            where T1 : DealUploadRequest
            where T2 : DealUploadResponse
        {
            var alerts = new List<Alert>();
            // Send request to Aspire
            try
            {
                Task timeoutTask = Task.Delay(_aspireRequestTimeout);
                var aspireRequestTask = aspireRequest(request);
                T2 response = null;
                if (await Task.WhenAny(aspireRequestTask, timeoutTask).ConfigureAwait(false) == aspireRequestTask)
                {
                    response = await aspireRequestTask.ConfigureAwait(false);
                }
                else
                {
                    throw new TimeoutException("External system operation has timed out.");
                }
                bool updated = false;
                //analyze response
                if (analyzeResponse != null)
                {
                    var aResult = analyzeResponse(response, analyzeInput);
                    if (aResult?.Item2?.Any() == true)
                    {
                        alerts.AddRange(aResult.Item2);
                    }
                    updated |= aResult?.Item1 == true;
                }
                if (postAnalyze != null)
                {
                    try
                    {
                        updated |= postAnalyze(response, postAnalyzeInput);
                    }
                    catch (Exception ePost)
                    {
                        alerts.Add(new Alert()
                        {
                            Code = ErrorCodes.AspireConnectionFailed,
                            Header = "Failed to execute postAction analyze after communicate with Aspire",
                            Type = AlertType.Error,
                            Message = ePost.ToString()
                        });
                        _loggingService.LogError("Failed to execute postAction analyze after communicate with Aspire", ePost);
                    }
                }
                if (updated)
                {
                    _unitOfWork.Save();
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.AspireConnectionFailed,
                    Header = ErrorConstants.AspireConnectionFailed,
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });
                _loggingService.LogError("Failed to communicate with Aspire", ex);
            }
            return alerts;
        }

        private Tuple<RequestHeader, IList<Alert>> GetAspireUser(string contractOwnerId)
        {
            var alerts = new List<Alert>();
            RequestHeader header = null;            

            try
            {
                var dealer = _contractRepository.GetDealer(contractOwnerId);
                string aspirePassword = null;
                if (!string.IsNullOrEmpty(dealer.AspireLogin))
                {
                    aspirePassword = _usersService.GetUserPassword(contractOwnerId);
                    if (string.IsNullOrEmpty(aspirePassword))
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Warning,
                            Header = "Cannot get user password",
                            Message = $"Cannot get password for user {contractOwnerId}"
                        });
                        _loggingService.LogWarning($"Cannot get password for user {contractOwnerId}");
                    }
                }

                if (!string.IsNullOrEmpty(aspirePassword))                    
                {
                    header = new RequestHeader()
                    {
                        UserId = dealer.AspireLogin,
                        Password = aspirePassword
                    };
                }
                else
                {
                    header = new RequestHeader()
                    {
                        UserId = _configuration.GetSetting(WebConfigKeys.ASPIRE_USER_CONFIG_KEY),
                        Password = _configuration.GetSetting(WebConfigKeys.ASPIRE_PASSWORD_CONFIG_KEY)
                    };
                }
            }
            catch (Exception ex)
            {
                var errorMsg = "Can't obtain user credentials";
                alerts.Add(new Alert()
                {
                    Header = errorMsg,
                    Message = errorMsg,
                    Type = AlertType.Error
                });
                _loggingService.LogError("Can't obtain Aspire user credentials", ex);
            }

            return new Tuple<RequestHeader, IList<Alert>>(header, alerts);
        }        

        private List<Account> GetCustomersInfo(Domain.Contract contract, string leadSource = null, bool withSymbolsMaping = false)
        {
            const string CustRole = "CUST";
            const string GuarRole = "GUAR";

            var accounts = new List<Account>();
            //var portalDescriber = _configuration.GetSetting($"PortalDescriber.{contract.Dealer?.ApplicationId}");

            Func<Domain.Customer, bool, string, Account> fillAccount = (c, isBorrower, role) =>
            {
                var account = new Account
                {
                    IsIndividual = true,
                    IsPrimary = c.IsDeleted != true,
                    Legalname = contract.Dealer?.Application?.LegalName,
                    EmailAddress = c.Emails?.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ??
                                   c.Emails?.FirstOrDefault()?.EmailAddress,
                    CreditReleaseObtained = true,
                    Personal = new Personal()
                    {
                        Firstname = c.FirstName.MapFrenchSymbols(withSymbolsMaping),
                        Lastname = c.LastName.MapFrenchSymbols(withSymbolsMaping),
                        Dob = c.DateOfBirth.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))
                    },
                };
                
                var location = c.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress);
                if (location == null)
                {
                    // try to get primary customer location
                    location = contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress) ??
                                      contract.PrimaryCustomer?.Locations?.FirstOrDefault();
                }
                location = location ?? c.Locations?.FirstOrDefault();

                if (location != null)
                {                    
                    account.Address = new Address()
                    {
                        City = location.City.MapFrenchSymbols(withSymbolsMaping),
                        Province = new Province()
                        {
                            Abbrev = location.State.ToProvinceCode()
                        },
                        Postalcode = location.PostalCode,
                        Country = new Country()
                        {
                            Abbrev = AspireUdfFields.DefaultAddressCountry
                        },
                        StreetName = location.Street.MapFrenchSymbols(withSymbolsMaping),
                        SuiteNo = location.Unit,
                        StreetNo = string.Empty
                    };                    
                }
                
                if (c.Phones?.Any() ?? false)
                {
                    var phone = c.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell)?.PhoneNum ??
                                c.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Home)?.PhoneNum ??
                                c.Phones.FirstOrDefault()?.PhoneNum;
                    account.Telecomm = new Telecomm()
                    {
                        Phone = phone
                    };
                }
                if (c.Emails?.Any() ?? false)
                {
                    var email = c.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ??
                                c.Emails.FirstOrDefault()?.EmailAddress;
                    if (account.Telecomm == null)
                    {
                        account.Telecomm = new Telecomm();
                    }
                    account.Telecomm.Email = email;
                }

                string setLeadSource = !string.IsNullOrEmpty(leadSource) ? leadSource :
                                    (_configuration.GetSetting(WebConfigKeys.DEFAULT_LEAD_SOURCE_KEY) ??
                                    (_configuration.GetSetting($"PortalDescriber.{contract.Dealer?.ApplicationId}") ?? contract.Dealer?.Application?.LeadSource));

                bool? existingCustomer = null;
                if (string.IsNullOrEmpty(c.AccountId))
                {
                    //check user on Aspire
                    var postalCode = location.PostalCode;
                        // Deal-4616 - Guarantor query fails as postal code was null for Guarantor with same address.
                        //c.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)?.PostalCode ??
                        //c.Locations?.FirstOrDefault()?.PostalCode;
                    try
                    {
                        var aspireCustomer = _aspireStorageReader.FindCustomer(c.FirstName, c.LastName, c.DateOfBirth,
                            postalCode);//AutoMapper.Mapper.Map<CustomerDTO>();
                        if (!string.IsNullOrEmpty(aspireCustomer?.EntityId))
                        {
                            account.ClientId = aspireCustomer.EntityId.Trim();
                            c.ExistingCustomer = true;
                            existingCustomer = true;
                            //check lead source in aspire
                            if (!string.IsNullOrEmpty(aspireCustomer.LeaseSource))
                            {
                                setLeadSource = null;
                            }
                        }
                        else
                        {
                            c.ExistingCustomer = false;
                            existingCustomer = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError("Failed to get customer from Aspire", ex);
                        account.ClientId = null;
                    }
                }
                else
                {
                    account.ClientId = c.AccountId;
                    //check lead source in aspire
                    var aspireCustomer = _aspireStorageReader.GetCustomerById(c.AccountId);
                    if (!string.IsNullOrEmpty(aspireCustomer?.LeaseSource))
                    {
                        setLeadSource = null;
                    }
                } 
                
                account.UDFs = GetCustomerUdfs(c, location, setLeadSource, isBorrower,
                                             contract.HomeOwners?.Any(hw => hw.Id == c.Id) == true ? (bool?)true : null, existingCustomer).ToList();                

                if (!string.IsNullOrEmpty(role))
                {
                    account.Role = role;
                }

                return account;
            };

            if (contract.PrimaryCustomer != null)
            {
                var acc = fillAccount(contract.PrimaryCustomer, true, CustRole);
                //acc.IsPrimary = true;                
                accounts.Add(acc);
            }

            contract.SecondaryCustomers?.ForEach(c => accounts.Add(fillAccount(c, false, GuarRole)));
            return accounts;
        }

        private List<Account> GetDealerOnboardingAccounts(DealerInfo dealerInfo, string leadSource = null)
        {            
            var accounts = new List<Account>();
            accounts.Add(GetCompanyAccount(dealerInfo));
            if (dealerInfo.Owners?.Any() == true)
            {
                var companyOwners = GetCompanyOwnersAccounts(dealerInfo, leadSource);
                if (companyOwners.Any())
                {
                    accounts.Add(companyOwners.First());
                }
            }
            return accounts;
        }

        private Account GetCompanyAccount(DealerInfo dealerInfo)
        {
            var companyInfo = dealerInfo.CompanyInfo;
            const string companyRole = "OTHER";
            var account = new Account
            {
                ClientId = companyInfo.AccountId,
                Role = companyRole,
                IsIndividual = false,
                IsPrimary = true,
                Legalname = companyInfo.OperatingName, // Legalname tag populates Name field in aspire and Business have requested to populate OperatingName in this field. Please refer Deal-4266 Link: https://support.dataart.com/browse/DEAL-4266       
                //Dba = companyInfo.OperatingName,
                EmailAddress = companyInfo.EmailAddress,                
                CreditReleaseObtained = true,
                Address = new Address()
                {
                    City = companyInfo.CompanyAddress?.City,
                    Province = new Province()
                    {
                        Abbrev = companyInfo.CompanyAddress?.State.ToProvinceCode()
                    },
                    Postalcode = companyInfo.CompanyAddress?.PostalCode,
                    Country = new Country()
                    {
                        Abbrev = AspireUdfFields.DefaultAddressCountry
                    },
                    StreetName = companyInfo.CompanyAddress?.Street,
                    SuiteNo = companyInfo.CompanyAddress?.Unit,
                    StreetNo = string.Empty
                },
                Telecomm = new Telecomm()
                {
                    Phone = companyInfo.Phone,
                    Email = companyInfo.EmailAddress,
                    Website = companyInfo.Website
                },
                UDFs = GetCompanyUdfs(dealerInfo).ToList()
            };
            return account;
        }

        private IList<Account> GetCompanyOwnersAccounts(DealerInfo dealerInfo, string leadSource = null)
        {
            const string ownerRole = "CUST";
            var accounts = dealerInfo.Owners?.OrderBy(o => o.OwnerOrder).Select(owner =>
            {
                var account = new Account
                {
                    Role = ownerRole,
                    IsIndividual = true,
                    IsPrimary = true,                    
                    Legalname = $"{owner.FirstName} {owner.LastName}",
                    EmailAddress = owner.EmailAddress,
                    CreditReleaseObtained = true,

                    Personal = new Personal()
                    {
                        Firstname = owner.FirstName,
                        Lastname = owner.LastName,
                        Dob = owner.DateOfBirth?.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))
                    },
                    Address = new Address()
                    {
                        City = owner.Address?.City,
                        Province = new Province()
                        {
                            Abbrev = owner.Address?.State.ToProvinceCode()
                        },
                        Postalcode = owner.Address?.PostalCode,
                        Country = new Country()
                        {
                            Abbrev = AspireUdfFields.DefaultAddressCountry
                        },
                        StreetName = owner.Address?.Street,
                        SuiteNo = owner.Address?.Unit,
                        StreetNo = string.Empty
                    },
                    Telecomm = new Telecomm()
                    {
                        Phone = owner.MobilePhone ?? owner.HomePhone,
                        Email = owner.EmailAddress
                    }
                };

                var setLeadSource = !string.IsNullOrEmpty(leadSource) ? leadSource : CultureHelper.CurrentCultureType == CultureType.French ? _configuration.GetSetting(WebConfigKeys.ONBOARDING_LEAD_SOURCE_FRENCH_KEY) : _configuration.GetSetting(WebConfigKeys.ONBOARDING_LEAD_SOURCE_KEY) 
                                                                                    ?? _configuration.GetSetting(WebConfigKeys.DEFAULT_LEAD_SOURCE_KEY);
                if (string.IsNullOrEmpty(owner.AccountId))
                {
                    //check user on Aspire
                    var postalCode = owner.Address?.PostalCode;
                    try
                    {
                        var aspireCustomer = _aspireStorageReader.FindCustomer(owner.FirstName, owner.LastName,
                            owner.DateOfBirth ?? new DateTime(), postalCode);
                        if (aspireCustomer != null)
                        {
                            account.ClientId = aspireCustomer.EntityId?.Trim();
                            //check lead source in aspire
                            if (!string.IsNullOrEmpty(aspireCustomer.LeaseSource))
                            {
                                setLeadSource = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError("Failed to get customer from Aspire", ex);
                        account.ClientId = null;
                    }
                }
                else
                {
                    account.ClientId = owner.AccountId;
                    //check lead source in aspire
                    var aspireCustomer = _aspireStorageReader.GetCustomerById(account.ClientId);
                    if (!string.IsNullOrEmpty(aspireCustomer?.LeaseSource))
                    {
                        setLeadSource = null;
                    }
                }

                var UDFs = new List<UDF>();
                UDFs.Add(new UDF()
                {
                    Name = AspireUdfFields.HomePhoneNumber,
                    Value = !string.IsNullOrEmpty(owner.HomePhone) ? owner.HomePhone : BlankValue
                });
                UDFs.Add(new UDF()
                {
                    Name = AspireUdfFields.MobilePhoneNumber,
                    Value = !string.IsNullOrEmpty(owner.MobilePhone) ? owner.MobilePhone : BlankValue
                });
                if (!string.IsNullOrEmpty(setLeadSource))
                {
                    UDFs.Add(new UDF()
                    {
                        Name = AspireUdfFields.CustomerLeadSource,
                        Value = setLeadSource
                    });
                }
                if (UDFs.Any())
                {
                    account.UDFs = UDFs;
                }
                return account;
            }).ToList();
            return accounts ?? new List<Account>();
        }

        private Application GetContractApplication(Domain.Contract contract, ICollection<NewEquipment> newEquipments = null, bool? isFirstEquipment = null, string leadSource = null)
        {
            var application = new Application()
            {
                TransactionId = contract.Details?.TransactionId
            };

            bool bFirstEquipment = isFirstEquipment ?? true;

            if (contract.Equipment != null)
            {
                var equipments = newEquipments ?? contract.Equipment.NewEquipment;
                equipments?.ForEach(eq =>
                {
                    if (application.Equipments == null)
                    {
                        application.Equipments = new List<Equipment>();
                    }
                    if (eq.IsDeleted != true || !string.IsNullOrEmpty(eq.AssetNumber))
                    {
                        application.Equipments.Add(new Equipment()
                        {
                            Status = "new",
                            AssetNo = string.IsNullOrEmpty(eq.AssetNumber) ? null : eq.AssetNumber,
                            Quantity = "1",
                            Cost = GetEquipmentCost(contract, eq, bFirstEquipment)?.ToString(CultureInfo.InvariantCulture),
                            Description = GetEquipmentDescription(eq),
                            AssetClass = new AssetClass() { AssetCode = eq.Type },
                            UDFs = GetEquipmentUdfs(contract, eq).ToList()
                        });
                    }
                    if (!isFirstEquipment.HasValue && bFirstEquipment)
                    {
                        bFirstEquipment = false;
                    }
                });
                application.AmtRequested = contract.Equipment?.ValueOfDeal?.ToString();
                application.TermRequested = contract.Equipment.AmortizationTerm?.ToString();
                application.Notes = contract.Details?.Notes ?? contract.Equipment.Notes;
                //TODO: Implement finance program selection
                application.FinanceProgram = contract.Dealer?.Application?.FinanceProgram;//"EcoHome Finance Program";

                application.ContractType = contract.Equipment?.AgreementType == AgreementType.LoanApplication
                    ? "LOAN"
                    : "RENTAL";
            }
            application.UDFs = GetApplicationUdfs(contract, leadSource).ToList();

            return application;
        }

        private decimal? GetEquipmentCost(Contract contract, NewEquipment equipment, bool isFirstEquipment)
        {
            decimal? eqCost;
            if (equipment.IsDeleted == true)
            {
                eqCost = 0.0m;
            }
            else
            {                
                if (IsClarityProgram(contract))
                {
                    decimal? installPackagesCost =
                        contract.Equipment?.InstallationPackages?.Aggregate(0.0m,
                            (sum, ip) => sum + ip.MonthlyCost ?? 0.0m);

                    int eqCount = contract.Equipment?.NewEquipment?.Count(ne => ne.IsDeleted != true) ?? 0;
                    eqCost = installPackagesCost.HasValue && eqCount > 0 ? equipment.MonthlyCost + (installPackagesCost.Value / eqCount) : equipment.MonthlyCost;
                    var totalAmount = (decimal?)_contractRepository.GetContractPaymentsSummary(contract.Id, eqCost)?.LoanDetails?.PriceOfEquipmentWithHst;                    
                    if (isFirstEquipment && totalAmount.HasValue)
                    {
                        totalAmount = totalAmount - (decimal?) contract.Equipment?.DownPayment ?? 0.0m;
                    }
                    eqCost = totalAmount;
                }
                else
                {
                    var rate = _contractRepository.GetProvinceTaxRate(
                            (contract.PrimaryCustomer?.Locations.FirstOrDefault(
                                 l => l.AddressType == AddressType.MainAddress) ??
                             contract.PrimaryCustomer?.Locations.First())?.State.ToProvinceCode());
                    
                    eqCost = contract.Equipment.AgreementType == AgreementType.LoanApplication && equipment.Cost.HasValue
                        ? (isFirstEquipment
                            ? (equipment.Cost - ((contract.Equipment.DownPayment != null) ? (decimal)contract.Equipment.DownPayment : 0))
                            : equipment.Cost)
                        : equipment.MonthlyCost * (1 + ((decimal?)rate?.Rate ?? 0.0m) / 100);
                }
                var adminFee = contract.Details?.AgreementType == AgreementType.LoanApplication && contract.Equipment?.IsFeePaidByCutomer == true
                    ? contract.Equipment?.AdminFee : null;
                if (isFirstEquipment && eqCost.HasValue && adminFee.HasValue)
                {
                    eqCost += adminFee;
                }

            }            
            return eqCost;
        }

        private string GetEquipmentDescription(NewEquipment equipment)
        {
            return equipment.IsDeleted != true
                ? (string.IsNullOrEmpty(equipment.EquipmentSubType?.Description)
                    ? equipment.Description
                    : $"{equipment.EquipmentSubType?.Description}-{equipment.Description}")
                : BlankValue;
        }

        private bool IsClarityProgram(Contract contract)
        {
            return _contractRepository.IsClarityProgram(contract.Id);
            //return contract?.Dealer?.Tier?.Name == _configuration.GetSetting(WebConfigKeys.CLARITY_TIER_NAME);
        }

        /// <summary>
        /// UDFs and some basic info only
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        private Application GetSimpleContractApplication(Domain.Contract contract, string leadSource = null, ContractorDTO contractor = null)
        {
            var application = new Application()
            {
                TransactionId = contract.Details?.TransactionId,                
            };

            if (contract.Equipment != null)
            {                
                application.AmtRequested = contract.Equipment.AmortizationTerm?.ToString();
                application.TermRequested = contract.Equipment.RequestedTerm?.ToString();

                application.ContractType = contract.Equipment?.AgreementType == AgreementType.LoanApplication
                    ? "LOAN"
                    : "RENTAL";                
            }

            application.Notes = contract.Details?.Notes ?? contract.Equipment?.Notes;
            //TODO: Implement finance program selection
            application.FinanceProgram = contract.Dealer?.Application?.FinanceProgram;//"EcoHome Finance Program";
            application.UDFs = GetApplicationUdfs(contract, leadSource, contractor).ToList();

            if (contractor != null)
            {
                application.UDFs.AddRange(GetContractorUdfs(contractor));
            }

            return application;
        }

        private Tuple<bool, IList<Alert>> AnalyzeResponse(DealUploadResponse response, Domain.Contract contract, ICollection<NewEquipment> newEquipments = null)
        {
            bool updated = false;
            var alerts = new List<Alert>();

            if (response?.Header == null || response.Header.Code != CodeSuccess || !string.IsNullOrEmpty(response.Header.ErrorMsg))
            {
                alerts.Add(new Alert()
                {
                    Header = response.Header.Status,
                    Message = response.Header.Message ?? response.Header.ErrorMsg,
                    Type = AlertType.Error
                });                
            }
            else
            {
                if (response.Payload != null)
                {
                    if (!string.IsNullOrEmpty(response.Payload.TransactionId) && contract.Details != null && contract.Details.TransactionId != response.Payload?.TransactionId)
                    {
                        contract.Details.TransactionId = response.Payload?.TransactionId;
                        updated = true;
                        _loggingService.LogInfo($"Aspire transaction Id [{response.Payload?.TransactionId}] created for contract [{contract.Id}]");
                    }

                    if (!string.IsNullOrEmpty(response.Payload.ContractStatus) && contract.Details != null &&
                        contract.Details.Status != response.Payload.ContractStatus)
                    {
                        contract.Details.Status = response.Payload.ContractStatus;
                        var aspireStatus = _contractRepository.GetAspireStatus(response.Payload.ContractStatus);
                        if (aspireStatus?.ContractState == ContractState.Closed &&
                            contract.ContractState != ContractState.Closed)
                        {
                            contract.ContractState = ContractState.Closed;
                            contract.LastUpdateTime = DateTime.UtcNow;
                        }
                        updated = true;
                        _loggingService.LogInfo($"Contract [{contract.Id}] state was changed to [{response.Payload.ContractStatus}]");
                    }

                    if (response.Payload.Accounts?.Any() ?? false)                        
                    {
                        var idUpdated = false;
                        response.Payload.Accounts.ForEach(a =>
                        {
                            if (a.Name.Contains(contract.PrimaryCustomer.FirstName) &&
                                    a.Name.Contains(contract.PrimaryCustomer.LastName) && contract.PrimaryCustomer.AccountId != a.Id)
                            {
                                contract.PrimaryCustomer.AccountId = a.Id;
                                idUpdated = true;
                            }

                            contract?.SecondaryCustomers.ForEach(c =>
                            {
                                if (a.Name.Contains(c.FirstName) &&
                                    a.Name.Contains(c.LastName) && c.AccountId != a.Id)
                                {
                                    c.AccountId = a.Id;
                                    idUpdated = true;
                                }
                            });
                        });

                        if (idUpdated)
                        {
                            updated = idUpdated;
                            _loggingService.LogInfo($"Aspire accounts created for {response.Payload.Accounts.Count} customers");
                        }
                    }                                        
                }                
            }      

            return new Tuple<bool, IList<Alert>>(updated, alerts);
        }

        private Tuple<bool, IList<Alert>> AnalyzeDealerUploadResponse(DealUploadResponse response, DealerInfo dealerInfo)
        {
            var alerts = new List<Alert>();
            bool idUpdated = false;

            if (response?.Header == null || response.Header.Code != CodeSuccess ||
                !string.IsNullOrEmpty(response.Header.ErrorMsg))
            {
                alerts.Add(new Alert()
                {
                    Header = response?.Header?.Status,
                    Message = response?.Header?.Message ?? response?.Header?.ErrorMsg,
                    Type = AlertType.Error
                });
            }
            else
            {
                if (response.Payload != null)
                {
                    if (!string.IsNullOrEmpty(response.Payload.TransactionId) && response.Payload.TransactionId != "0")
                    {                        
                        dealerInfo.TransactionId = response.Payload?.TransactionId;
                        _unitOfWork.Save();
                        _loggingService.LogInfo($"Aspire transaction Id [{response.Payload?.TransactionId}] created for dealer onboarding form");
                    }

                    if (!string.IsNullOrEmpty(response.Payload.ContractStatus) && dealerInfo.Status != response.Payload.ContractStatus)
                    {
                        dealerInfo.Status = response.Payload.ContractStatus;
                        _unitOfWork.Save();
                    }

                    if (response.Payload.Accounts?.Any() ?? false)
                    {
                        response.Payload.Accounts.ForEach(a =>
                        {
                            if (dealerInfo.CompanyInfo != null && a.Name.Contains(dealerInfo.CompanyInfo?.OperatingName) && dealerInfo.CompanyInfo?.AccountId != a.Id)
                            {
                                dealerInfo.CompanyInfo.AccountId = a.Id;
                                idUpdated = true;
                            }

                            dealerInfo?.Owners.ForEach(c =>
                            {
                                if (a.Name.Contains(c.FirstName) &&
                                    a.Name.Contains(c.LastName) && c.AccountId != a.Id)
                                {
                                    c.AccountId = a.Id;
                                    idUpdated = true;
                                }
                            });                            
                        });

                        if (idUpdated)
                        {
                            _unitOfWork.Save();
                            _loggingService.LogInfo($"Aspire accounts created for {response.Payload.Accounts.Count} dealer onboarding");
                        }
                    }
                }
            }
            return new Tuple<bool, IList<Alert>>(idUpdated, alerts);
        }

        private CreditCheckDTO GetCreditCheckResult(DealUploadResponse response)
        {
            CreditCheckDTO checkResult = new CreditCheckDTO();

            int scorePoints = 0;

            if (!string.IsNullOrEmpty(response?.Payload?.ScorecardPoints) &&
                int.TryParse(response.Payload.ScorecardPoints, out scorePoints))
            {
                checkResult.ScorecardPoints = scorePoints;

                if (scorePoints > 180 && scorePoints <= 220)
                {
                    checkResult.CreditAmount = 15000;
                }
                if (scorePoints > 220 && scorePoints < 1000)
                {
                    checkResult.CreditAmount = 20000;
                }
            }

            checkResult.CreditCheckState = CreditCheckState.Initiated;

            bool passFail = true;
            bool.TryParse(response?.Payload?.ScorecardPassFail, out passFail);

            const string Approved = "Approved";
            const string Declined = "Declined";
            string[] CreditReview = { "Credit Review", "MIR", "Submitted" };

            if (!string.IsNullOrEmpty(response?.Payload?.ContractStatus) && !passFail)
            {
                if (response.Payload.ContractStatus.Contains(Approved))
                {
                    checkResult.CreditCheckState = CreditCheckState.Approved;
                }
                if (response.Payload.ContractStatus.Contains(Declined))
                {
                    checkResult.CreditCheckState = CreditCheckState.Declined;
                }
                if (CreditReview.Any(c => response.Payload.ContractStatus.Contains(c)))
                {
                    checkResult.CreditCheckState = CreditCheckState.MoreInfoRequired;
                }
            }

            return checkResult;
        }

        private IList<UDF> GetEquipmentUdfs(Domain.Contract contract, NewEquipment equipment)
        {
            var udfList = new List<UDF>();
            decimal? eqCost = 0.0m;
            if (equipment.IsDeleted != true && IsClarityProgram(contract))
            {
                decimal? installPackagesCost =
                    contract.Equipment?.InstallationPackages?.Aggregate(0.0m,
                        (sum, ip) => sum + ip.MonthlyCost ?? 0.0m);

                var paymentSummary = _contractRepository.GetContractPaymentsSummary(contract.Id);
                var paymentFactor = paymentSummary.LoanDetails.TotalMCO != 0.0 ? paymentSummary.LoanDetails.TotalMCO / paymentSummary.LoanDetails.TotalMonthlyPayment
                        : 1.0;
                int eqCount = contract.Equipment?.NewEquipment?.Count(ne => ne.IsDeleted != true) ?? 0;
                eqCost = installPackagesCost.HasValue && eqCount > 0
                    ? equipment.MonthlyCost + (installPackagesCost.Value / eqCount)
                    : equipment.MonthlyCost;
                eqCost = (eqCost ?? 0m ) * (decimal)paymentFactor;

                udfList.Add(new UDF
                {
                    Name = AspireUdfFields.MonthlyPayment,
                    Value = eqCost?.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                });
            }
            else
            {
                udfList.Add(new UDF
                {
                    Name = AspireUdfFields.EstimatedRetailPrice,
                    Value = equipment.EstimatedRetailCost?.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                });
                //DEAL-5092 - it is an issue in Aspire. We cant send more that 1 UDF for equipment at the moment
                //udfList.Add(new UDF
                //{
                //    Name = AspireUdfFields.MonthlyPayment,
                //    Value = "0.0"
                //});
            }
            return udfList;
        }

        private IList<UDF> GetApplicationUdfs(Domain.Contract contract, string leadSource = null, ContractorDTO contractor = null)
        {
            var udfList = new List<UDF>();
            if (contract?.Equipment != null)
            {
                if (contract.Equipment.DeferralType != null)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.DeferralType,
                        Value = contract.Equipment.DeferralType.GetPersistentEnumDescription()
                    });
                }
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RequestedTerm,
                    Value = (contract.Details.AgreementType == AgreementType.LoanApplication ? contract.Equipment.LoanTerm?.ToString() : contract.Equipment.RequestedTerm?.ToString()) ?? BlankValue
                });

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.AmortizationTerm,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        contract.Equipment.AmortizationTerm?.ToString() ?? "0" : "0"
                });

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RentalProgramType,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ? "0" :
                       !contract.Equipment.RentalProgramType.HasValue ? "0":
                       contract.Equipment.RentalProgramType.Value == AnnualEscalationType.Escalation0  ?
                       "13.99" : "10.99"
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ContractRentalRate,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ? "0" :
                        !contract.Equipment.RentalProgramType.HasValue ? "0" :
                            contract.Equipment.RentalProgramType.Value == AnnualEscalationType.Escalation0 ?
                                "13.99" : "10.99"
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ContractEscalationRate,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ? "0" :
                        !contract.Equipment.RentalProgramType.HasValue ? "0" :
                            contract.Equipment.RentalProgramType.Value == AnnualEscalationType.Escalation0 ?
                                "0" : "3.5"
                });

                if (contract.Details.AgreementType == AgreementType.LoanApplication && contract.Equipment.LoanTerm.HasValue)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.TermType,
                        Value = $"{contract.Equipment.LoanTerm.Value} Months"
                    });
                }
                else
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.TermType,
                        Value = BlankValue
                    });
                }

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.AdminFee,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication && contract.Equipment?.IsFeePaidByCutomer == true ?
                        contract.Equipment?.RateCard?.AdminFee.ToString(CultureInfo.InvariantCulture) ?? contract.Equipment?.AdminFee?.ToString()
                        : "0.0"
                });

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.FeePaidBy,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        (contract.Equipment.IsFeePaidByCutomer ?? false ? "C" : "D")
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.DownPayment,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                            contract.Equipment?.DownPayment?.ToString() ?? "0.0" : "0.0"
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.CustomerRate,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        contract.Equipment?.CustomerRate?.ToString(CultureInfo.InvariantCulture) ?? "0.0"
                        : "0.0"
                });

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RateCardType,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        contract.Equipment?.RateCard?.CardType.GetEnumDescription() ?? "Custom"
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.DealerTierName,
                    Value = contract.Dealer?.Tier?.Name ?? BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RateReduction,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        (contract.Equipment.RateReduction.HasValue ? contract.Equipment.RateReduction.Value.ToString() : "0.0")
                        : "0.0"
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RateReductionCost,
                    Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                        (contract.Equipment.RateReductionCost.HasValue ? contract.Equipment.RateReductionCost.Value.ToString("F", CultureInfo.InvariantCulture) 
                        : "0.0")
                        : "0.0"
                });

                var creditAmount = contract.Details?.CreditAmount ?? 0.0m;

                if (contract.Dealer?.Tier?.IsCustomerRisk == true && contract.PrimaryCustomer?.CreditReport == null)
                {
                    creditAmount = 0.0m;
                }

                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ContractPreapprovalLimit,
                    Value = creditAmount.ToString("F", CultureInfo.InvariantCulture)
                });
                var beacon = contract.PrimaryCustomer?.CreditReport?.Beacon ?? 0;
                if (contract.Dealer?.Tier?.IsCustomerRisk == true)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.CustomerRiskGroup,
                        Value = _rateCardsRepository.GetCustomerRiskGroupByBeacon(beacon).GroupName ?? BlankValue
                    });
                }
          
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.CustomerHasExistingAgreements,
                    Value = contract.Equipment?.HasExistingAgreements.HasValue == true ? 
                        (contract.Equipment.HasExistingAgreements == true ? "Y" : "N")
                        : BlankValue
                });

                var paymentInfo = _contractRepository.GetContractPaymentsSummary(contract.Id);
                if (paymentInfo != null)
                {
                    if (contract.Details?.AgreementType == AgreementType.LoanApplication)
                    {
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.MonthlyPayment,
                            Value = paymentInfo.LoanDetails?.TotalMCO.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.RentalMonthlyPayment,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.ContractSoftCapLimit,
                            Value = BlankValue
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.BorrowingCost,
                            Value = paymentInfo.LoanDetails?.TotalBorowingCost.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.ResidualValue,
                            Value = paymentInfo.LoanDetails?.ResidualBalance.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.TotalObligation,
                            Value = paymentInfo.LoanDetails?.TotalObligation.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                        });
                        var dealerCostRate = (decimal?)contract.Equipment?.RateCard?.DealerCost ?? contract.Equipment?.DealerCost;
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.DealerRate,
                            Value = dealerCostRate?.ToString(CultureInfo.InvariantCulture) ?? "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.DealerCost,
                            Value = dealerCostRate.HasValue ?
                                ((decimal)dealerCostRate.Value / 100 * paymentInfo.TotalAmountFinanced ?? 0.0m).ToString(CultureInfo.InvariantCulture)
                                : "0.0"
                        });                        
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.CustomerApr,
                            Value = _contractRepository.IsClarityProgram(contract.Id) ? "0.0" :
                                (contract.Equipment?.IsFeePaidByCutomer == true  ? 
                                    (paymentInfo.LoanDetails?.AnnualPercentageRate.ToString("F", CultureInfo.InvariantCulture) ?? "0.0")
                                    : (contract.Equipment.CustomerRate?.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"))
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.TotalEquipmentPrice,
                            Value = paymentInfo.LoanDetails.LoanTotalCashPrice.ToString("F", CultureInfo.InvariantCulture)                            
                        });
                    }
                    else
                    {
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.MonthlyPayment,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.RentalMonthlyPayment,
                            Value = paymentInfo.TotalMonthlyPayment?.ToString() ?? "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.ContractSoftCapLimit,
                            Value = paymentInfo.SoftCapLimit ? "Y": "N"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.BorrowingCost,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.ResidualValue,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.TotalObligation,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.DealerRate,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.DealerCost,
                            Value = "0.0"
                        });
                                                
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.CustomerApr,
                            Value = "0.0"
                        });
                        udfList.Add(new UDF()
                        {
                            Name = AspireUdfFields.TotalEquipmentPrice,
                            Value = "0.0"
                        });
                    }                    

                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.TotalAmountFinanced,
                        Value = contract.Details.AgreementType == AgreementType.LoanApplication ? 
                            paymentInfo.TotalAmountFinanced?.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                            : "0.0"
                    });
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.TotalOfAllMonthlyPayment,
                        Value = contract.Details.AgreementType == AgreementType.LoanApplication ?
                            paymentInfo.TotalAllMonthlyPayment?.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                            : "0.0"
                    });                                       
                }

                var taxRate = _contractRepository.GetProvinceTaxRate((contract.PrimaryCustomer?.Locations.FirstOrDefault(
                                                                          l => l.AddressType == AddressType.MainAddress) ??
                                                                      contract.PrimaryCustomer?.Locations.First())?.State.ToProvinceCode());
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PstRate,
                    Value = contract.Details?.AgreementType == AgreementType.LoanApplication && !IsClarityProgram(contract) ?
                        "0.0"
                        : ((taxRate?.Rate ?? 0.0) / 100).ToString(CultureInfo.InvariantCulture) ?? "0.0"
                });

                if (!string.IsNullOrEmpty(contract.Equipment.SalesRep))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.DealerSalesRep,
                        Value = contract.Equipment.SalesRep
                    });
                }
                if (contract.SalesRepInfo != null)
                {
                    var roles = new List<string>();
                    if (contract.SalesRepInfo.ConcludedAgreement)
                    {
                        roles.Add("Concluded Agreement");
                    }
                    if (contract.SalesRepInfo.InitiatedContact)
                    {
                        roles.Add("Initiated Contact");
                    }
                    if (contract.SalesRepInfo.NegotiatedAgreement)
                    {
                        roles.Add("Negotiated Agreement");
                    }
                    var rolesStr = string.Join(", ", roles);
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.DealerSalesRepRoles,
                        Value = !string.IsNullOrEmpty(rolesStr) ? rolesStr : BlankValue
                    });
                }
                else
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.DealerSalesRepRoles,
                        Value = BlankValue
                    });
                }
                if (contract.Equipment.EstimatedInstallationDate.HasValue)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.PreferredInstallationDate,
                        Value = contract.Equipment.EstimatedInstallationDate.Value.Hour > 0 ? contract.Equipment.EstimatedInstallationDate.Value.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))
                            : contract.Equipment.EstimatedInstallationDate.Value.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))
                    });
                }
                if (contract.Equipment.PreferredStartDate.HasValue)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.PreferredDateToStartProject,
                        Value = contract.Equipment.PreferredStartDate.Value.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))
                    });
                }
                if (contract.Equipment.NewEquipment?.Where(ne => ne.IsDeleted != true).Any() == true)
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.HomeImprovementType,
                        Value = contract.Equipment.NewEquipment.First(ne => ne.IsDeleted != true).Type
                    });
                }

                if (IsClarityProgram(contract))
                {
                    var paymentFactor = (paymentInfo?.LoanDetails.TotalMCO / paymentInfo?.LoanDetails.TotalMonthlyPayment) ?? 1.0;

                    const int maxPackages = 3;
                    int packageNum = 1;
                    var packages = contract.Equipment?.InstallationPackages?.Take(3).ToList() ?? new List<InstallationPackage>();
                    for (int i = packages.Count(); i < maxPackages; i++)
                    {
                        packages.Add(null);
                    }

                    packages.ForEach(ip =>
                    {
                        udfList.Add(new UDF()
                        {
                            Name = $"{AspireUdfFields.InstallPackageDescr}{packageNum}",
                            Value = ip?.Description ?? BlankValue
                        });
                        udfList.Add(new UDF()
                        {
                            Name = $"{AspireUdfFields.InstallMonthlyPay}{packageNum}",
                            Value = ((double)(ip?.MonthlyCost ?? 0) * paymentFactor).ToString("F", CultureInfo.InvariantCulture)
                        });
                        packageNum++;
                    });
                }
            }            

            if (contract?.PaymentInfo != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PaymentType,
                    Value = contract.PaymentInfo?.PaymentType == PaymentType.Enbridge ? "Enbridge" : "PAD"
                });
                if (contract.PaymentInfo.PaymentType == PaymentType.Enbridge &&
                    (!string.IsNullOrEmpty(contract.PaymentInfo?.EnbridgeGasDistributionAccount) ||
                     !string.IsNullOrEmpty(contract.PaymentInfo?.MeterNumber)))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.EnbridgeGasAccountNumber,
                        Value = contract.PaymentInfo.EnbridgeGasDistributionAccount ?? contract.PaymentInfo.MeterNumber
                    });
                }
                else
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.EnbridgeGasAccountNumber,
                        Value = BlankValue
                    });
                }
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EnbridgeMeter,
                    Value = contract.PaymentInfo.PaymentType == PaymentType.Enbridge ? contract.PaymentInfo.MeterNumber : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PapAccountNumber,
                    Value = contract.PaymentInfo.PaymentType == PaymentType.Pap ? contract.PaymentInfo.AccountNumber ?? BlankValue : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PapTransitNumber,
                    Value = contract.PaymentInfo.PaymentType == PaymentType.Pap ? contract.PaymentInfo.TransitNumber ?? BlankValue : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PapBankNumber,
                    Value = contract.PaymentInfo.PaymentType == PaymentType.Pap ? contract.PaymentInfo.BlankNumber ?? BlankValue : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.PapWithdrawalDate,
                    Value = (contract.PaymentInfo.PaymentType == PaymentType.Pap 
                                && contract.PrimaryCustomer?.Locations?.FirstOrDefault(m => m.AddressType == AddressType.MainAddress)?.State != "QC") 
                        ? (contract.PaymentInfo.PrefferedWithdrawalDate == WithdrawalDateType.First  ? "1" : "15")
                        : BlankValue
                });                          
            }            

            if (!string.IsNullOrEmpty(contract?.ExternalSubDealerId))
            {
                try
                {
                    var subDealers =
                        _aspireStorageReader.GetSubDealersList(contract.Dealer.AspireLogin ?? contract.Dealer.UserName);
                    var sbd = subDealers?.FirstOrDefault(sd => sd.SubmissionValue == contract.ExternalSubDealerId);
                    if (sbd != null)
                    {
                        udfList.Add(new UDF()
                        {
                            Name = sbd.DealerName,
                            Value = sbd.SubmissionValue
                        });
                    }
                }
                catch (Exception ex)
                {
                    //we can get error here from Aspire DB
                    _loggingService.LogError("Failed to get subdealers from Aspire", ex);
                }
            }
            var setLeadSource = !string.IsNullOrEmpty(leadSource)
                ? leadSource
                : (_configuration.GetSetting(WebConfigKeys.DEFAULT_LEAD_SOURCE_KEY) ??
                   (_configuration.GetSetting($"PortalDescriber.{contract.Dealer?.ApplicationId}") ??
                    contract.Dealer?.Application?.LeadSource));
            if (!string.IsNullOrEmpty(contract.Details.TransactionId))
            {
                //check lead source in aspire
                //setLeadSource = null;
            }
            if (!string.IsNullOrEmpty(setLeadSource))
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.LeadSource,
                    Value = setLeadSource
                });
            }            

            return udfList;
        }

        private IList<UDF> GetContractorUdfs(ContractorDTO contractor)
        {
            var udfList = new List<UDF>();
            if (contractor != null)
            {
                if (!string.IsNullOrEmpty(contractor.CompanyName))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorName,
                        Value = contractor.CompanyName
                    });
                }
                if (!string.IsNullOrEmpty(contractor.City))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorCity,
                        Value = contractor.City
                    });
                }
                if (!string.IsNullOrEmpty(contractor.EmailAddress))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorEmail,
                        Value = contractor.EmailAddress
                    });
                }
                if (!string.IsNullOrEmpty(contractor.PhoneNumber))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorPhone,
                        Value = contractor.PhoneNumber
                    });
                }
                if (!string.IsNullOrEmpty(contractor.PostalCode))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorPostalCode,
                        Value = contractor.PostalCode
                    });
                }
                if (!string.IsNullOrEmpty(contractor.State))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorProvince,
                        Value = contractor.State
                    });
                }
                if (!string.IsNullOrEmpty(contractor.Street))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorStreet,
                        Value = contractor.Street
                    });
                }
                if (!string.IsNullOrEmpty(contractor.Unit))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorUnit,
                        Value = contractor.Unit
                    });
                }
                if (!string.IsNullOrEmpty(contractor.Website))
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.ReqContractorWebsite,
                        Value = contractor.Website
                    });
                }
            }

            return udfList;
        }

        private IList<UDF> GetCustomerUdfs(Domain.Customer customer, Location mainLocation, string leadSource, bool isBorrower, bool? isHomeOwner = null, bool? existingCustomer = null)
        {
            var udfList = new List<UDF>();
            if (!string.IsNullOrEmpty(leadSource))
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.CustomerLeadSource,
                    Value = leadSource
                });
            }
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.Residence,
                Value = isHomeOwner == true ? "O" : BlankValue
                //Value = mainLocation.ResidenceType == ResidenceType.Own ? "O" : "R"
                //<!—other value is R for rent  and O for own-->
            });
            udfList.Add(
                new UDF()
                {

                    Name = AspireUdfFields.AuthorizedConsent,
                    Value = "Y"
                });
            if (existingCustomer.HasValue)
            {
                udfList.Add(
                    new UDF()
                    {

                        Name = AspireUdfFields.ExistingCustomer,
                        Value = existingCustomer.Value ? "Y" : "N"
                    });
            }

            var previousAddress = customer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.PreviousAddress);
            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddress,
                    Value = previousAddress?.Street ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressCity,
                    Value = previousAddress?.City ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressPostalCode,
                    Value = previousAddress?.PostalCode ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressState,
                    Value = previousAddress?.State?.ToProvinceCode() ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressCountry,
                    Value = previousAddress != null ? AspireUdfFields.DefaultAddressCountry : BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressUnit,
                    Value = previousAddress?.Unit ?? BlankValue
                }
            });

            var installationAddress = customer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress);
            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddress,
                    Value = installationAddress?.Street ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressCity,
                    Value = installationAddress?.City ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressPostalCode,
                    Value = installationAddress?.PostalCode ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressState,
                    Value = installationAddress?.State?.ToProvinceCode() ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressCountry,
                    Value = installationAddress != null ? AspireUdfFields.DefaultAddressCountry : BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.EstimatedMoveInDate,
                    Value = installationAddress?.MoveInDate?.ToString("d", CultureInfo.CreateSpecificCulture("en-US")) ?? BlankValue
                }
            });

            var mailingAddress = customer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MailAddress);
            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddress,
                    Value = mailingAddress?.Street ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressCity,
                    Value = mailingAddress?.City ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressPostalCode,
                    Value = mailingAddress?.PostalCode ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressState,
                    Value = mailingAddress?.State?.ToProvinceCode() ?? BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressCountry,
                    Value = mailingAddress != null ? AspireUdfFields.DefaultAddressCountry : BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressUnit,
                    Value = mailingAddress?.Unit ?? BlankValue
                }
            });            

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.HomeOwner,
                Value = isHomeOwner == true ? "Y" : "N"
            });

            if (!isBorrower)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RelationshipToCustomer,
                    Value = customer.RelationshipToMainBorrower ?? BlankValue
                });
            }

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.HomePhoneNumber,
                Value = customer.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Home)?.PhoneNum ?? BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MobilePhoneNumber,
                Value = customer.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Cell)?.PhoneNum ?? BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.BusinessPhoneNumber,
                Value = customer.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Business)?.PhoneNum ?? BlankValue
            });                        

            if (customer.AllowCommunicate.HasValue)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.AllowCommunicate,
                    Value = customer.AllowCommunicate.Value ? "1" : "0"
                });
            }

            if (customer.PreferredContactMethod.HasValue)
            {
                string contactMethod = null;
                switch (customer.PreferredContactMethod)
                {
                    case PreferredContactMethod.Email:
                        contactMethod = AspireUdfFields.ContactViaEmail;
                        break;
                    case PreferredContactMethod.Phone:
                        contactMethod = AspireUdfFields.ContactViaPhone;
                        break;
                    case PreferredContactMethod.Text:
                        contactMethod = AspireUdfFields.ContactViaText;
                        break;
                }
                if (contactMethod != null)
                {
                    udfList.Add(new UDF()
                    {
                        Name = contactMethod, Value = "Y"
                    });
                }
            }

            if (customer.EmploymentInfo != null)
            {
                if (customer.EmploymentInfo.EmploymentStatus.HasValue)
                {
                    string eStatus = null;
                    switch (customer.EmploymentInfo.EmploymentStatus)
                    {
                        case EmploymentStatus.Unemployed:
                            eStatus = "U";
                            break;
                        case EmploymentStatus.SelfEmployed:
                            eStatus = "S";
                            break;
                        case EmploymentStatus.Retired:
                            eStatus = "R";
                            break;
                        case EmploymentStatus.Employed:
                        default:
                            eStatus = "E";
                            break;

                    }
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.EmploymentStatus,
                        Value = eStatus
                    });
                }
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.MonthlyMortgage,
                    Value = customer.EmploymentInfo.MonthlyMortgagePayment.ToString(CultureInfo.InvariantCulture) ??
                            BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmploymentType,
                    Value = customer.EmploymentInfo.EmploymentType.HasValue
                        ? (customer.EmploymentInfo.EmploymentType == EmploymentType.FullTime ? "F" : "P")
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.IncomeType,
                    Value = customer.EmploymentInfo.IncomeType.HasValue
                        ? (customer.EmploymentInfo.IncomeType == IncomeType.HourlyRate ? "H" : "A")
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.JobTitle,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.JobTitle)
                        ? customer.EmploymentInfo.JobTitle
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmployerName,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.CompanyName)
                        ? customer.EmploymentInfo.CompanyName
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmployerPhone,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.CompanyPhone)
                        ? customer.EmploymentInfo.CompanyPhone
                        : BlankValue
                });
                if (customer.EmploymentInfo.CompanyAddress != null &&
                    (customer.EmploymentInfo.EmploymentStatus == EmploymentStatus.Employed ||
                     customer.EmploymentInfo.EmploymentStatus == EmploymentStatus.SelfEmployed))
                {
                    var cAddress = !string.IsNullOrEmpty(customer.EmploymentInfo.CompanyAddress.Unit)
                        ? $"{customer.EmploymentInfo.CompanyAddress.Street}, {Resources.Resources.Suite} {customer.EmploymentInfo.CompanyAddress.Unit}, {customer.EmploymentInfo.CompanyAddress.City}, {customer.EmploymentInfo.CompanyAddress.State}, {customer.EmploymentInfo.CompanyAddress.PostalCode}"
                        : $"{customer.EmploymentInfo.CompanyAddress.Street}, {customer.EmploymentInfo.CompanyAddress.City}, {customer.EmploymentInfo.CompanyAddress.State}, {customer.EmploymentInfo.CompanyAddress.PostalCode}";
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.EmployerAddress,
                        Value = !string.IsNullOrEmpty(cAddress) ? cAddress : BlankValue
                    });
                }
                else
                {
                    udfList.Add(new UDF()
                    {
                        Name = AspireUdfFields.EmployerAddress,
                        Value = BlankValue
                    });
                }
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.AnnualSalary,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.AnnualSalary)
                        ? customer.EmploymentInfo.AnnualSalary.Replace("$", "").Replace(" ", "")
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.HourlyRate,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.HourlyRate)
                        ? customer.EmploymentInfo.HourlyRate.Replace("$", "").Replace(" ", "")
                        : BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmploymentLength,
                    Value = !string.IsNullOrEmpty(customer.EmploymentInfo.LengthOfEmployment)
                        ? customer.EmploymentInfo.LengthOfEmployment
                        : BlankValue
                });
            }
            else
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmploymentStatus,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.MonthlyMortgage,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmploymentType,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.IncomeType,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.JobTitle,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmployerName,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmployerPhone,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmployerAddress,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.AnnualSalary,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.HourlyRate,
                    Value = BlankValue
                });
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.EmploymentLength,
                    Value = BlankValue
                });
            }

            return udfList;
        }

        private IList<UDF> GetCleanCustomerUdfs()
        {
            var udfList = new List<UDF>();
            
            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddress,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressCity,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressPostalCode,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressState,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressCountry,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.PreviousAddressUnit,
                    Value = BlankValue
                }
            });

            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddress,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressCity,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressPostalCode,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressState,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.InstallationAddressCountry,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.EstimatedMoveInDate,
                    Value = BlankValue
                }
            });

            udfList.AddRange(new UDF[]
            {
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddress,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressCity,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressPostalCode,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressState,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressCountry,
                    Value = BlankValue
                },
                new UDF()
                {
                    Name = AspireUdfFields.MailingAddressUnit,
                    Value = BlankValue
                }
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.HomePhoneNumber,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MobilePhoneNumber,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.BusinessPhoneNumber,
                Value = BlankValue
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.RelationshipToCustomer,
                Value = BlankValue
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmploymentStatus,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MonthlyMortgage,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmploymentType,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.IncomeType,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.JobTitle,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmployerName,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmployerPhone,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmployerAddress,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.AnnualSalary,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.HourlyRate,
                Value = BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.EmploymentLength,
                Value = BlankValue
            });

            return udfList;
        }

        private IList<UDF> GetCompanyUdfs(DealerInfo dealerInfo)
        {
            var udfList = new List<UDF>();

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.LegalName,
                Value = !string.IsNullOrEmpty(dealerInfo?.CompanyInfo?.FullLegalName) ? dealerInfo.CompanyInfo.FullLegalName : BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.OperatingName,
                Value = !string.IsNullOrEmpty(dealerInfo?.CompanyInfo?.OperatingName) ? dealerInfo.CompanyInfo.OperatingName : BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.NumberOfInstallers,
                Value = dealerInfo?.CompanyInfo?.NumberOfInstallers != null ? dealerInfo.CompanyInfo.NumberOfInstallers.GetEnumDescription()
                        : NumberOfPeople.Zero.GetEnumDescription()
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.NumberOfSalesPeople,
                Value = dealerInfo?.CompanyInfo?.NumberOfSales != null ? dealerInfo.CompanyInfo.NumberOfSales.GetEnumDescription()
                        : NumberOfPeople.Zero.GetEnumDescription()
            });
            if (dealerInfo?.CompanyInfo?.BusinessType != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.TypeOfBusiness,
                    Value = dealerInfo.CompanyInfo.BusinessType.GetEnumDescription()
                });
            }
            if (dealerInfo?.CompanyInfo?.YearsInBusiness != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.YearsInBusiness,
                    Value = dealerInfo.CompanyInfo.YearsInBusiness.GetEnumDescription()
                });
            }
            if (dealerInfo?.CompanyInfo?.Provinces?.Any() == true)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ProvincesApproved,
                    Value = GetCompanyProvincesApproved(dealerInfo)
                });
            }
            else
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ProvincesApproved,
                    Value = BlankValue
                });
            }

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.Website,
                Value = !string.IsNullOrEmpty(dealerInfo?.CompanyInfo?.Website) ? dealerInfo.CompanyInfo.Website : BlankValue
            });

            if (dealerInfo.ProductInfo?.Brands?.Any() == true || !string.IsNullOrEmpty(dealerInfo.ProductInfo?.PrimaryBrand))
            {
                var brandsList = new List<string>();
                if (!string.IsNullOrEmpty(dealerInfo.ProductInfo?.PrimaryBrand))
                {
                    brandsList.Add(dealerInfo.ProductInfo.PrimaryBrand);
                }
                if (dealerInfo.ProductInfo?.Brands?.Any() == true)
                {
                    brandsList.AddRange(dealerInfo.ProductInfo.Brands.Select(b => b.Brand));
                }
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ManufacturerBrandsSold,
                    Value = string.Join(", ", brandsList)
                });
            }
            else
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ManufacturerBrandsSold,
                    Value = BlankValue
                });
            }

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.AnnualSalesVolume,
                Value = dealerInfo.ProductInfo?.AnnualSalesVolume != null ? dealerInfo.ProductInfo.AnnualSalesVolume?.ToString(CultureInfo.InvariantCulture) : BlankValue
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.AverageTransactionSize,
                Value = dealerInfo.ProductInfo?.AverageTransactionSize != null ? dealerInfo.ProductInfo.AverageTransactionSize?.ToString(CultureInfo.InvariantCulture) : BlankValue
            });

            if (dealerInfo.ProductInfo?.LeadGenReferrals != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.LeadGeneratedWithReferrals,
                    Value = dealerInfo.ProductInfo.LeadGenReferrals == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.LeadGenLocalAdvertising != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.LeadGeneratedWithLocalAdvertising,
                    Value = dealerInfo.ProductInfo.LeadGenLocalAdvertising == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.LeadGenTradeShows != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.LeadGeneratedWithTradeShows,
                    Value = dealerInfo.ProductInfo.LeadGenTradeShows == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.SalesApproachBroker != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ChannelTypeBroker,
                    Value = dealerInfo.ProductInfo.SalesApproachBroker == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.SalesApproachConsumerDirect != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ChannelTypeConsumerDirect,
                    Value = dealerInfo.ProductInfo.SalesApproachConsumerDirect == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.SalesApproachDistributor != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ChannelTypeDistributor,
                    Value = dealerInfo.ProductInfo.SalesApproachDistributor == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.SalesApproachDoorToDoor != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ChannelTypeDoorToDoorSales,
                    Value = dealerInfo.ProductInfo.SalesApproachDoorToDoor == true ? "Y" : "N"
                });
            }            
            if (dealerInfo.ProductInfo?.ProgramService != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ProgramServicesRequired,
                    Value = dealerInfo.ProductInfo.ProgramService == ProgramServices.Both ? "Financing + Leasing" : (dealerInfo.ProductInfo.ProgramService == ProgramServices.Loan ? "Financing" :"Leasing" )
                });
            }
            if (dealerInfo.ProductInfo?.Relationship != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.RelationshipStructure,
                    Value = dealerInfo.ProductInfo.Relationship.GetEnumDescription()
                });
            }
            if (dealerInfo.ProductInfo?.WithCurrentProvider != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.CurrentFinanceProvider,
                    Value = dealerInfo.ProductInfo.WithCurrentProvider == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.OfferMonthlyDeferrals != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.OfferDeferrals,
                    Value = dealerInfo.ProductInfo.OfferMonthlyDeferrals == true ? "Y" : "N"
                });
            }
            if (dealerInfo.ProductInfo?.ReasonForInterest != null)
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ReasonForInterest,
                    Value = dealerInfo.ProductInfo.ReasonForInterest.GetEnumDescription()
                });
            }
            if (dealerInfo.ProductInfo?.Services?.Any() == true)
            {                
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ProductsForFinancingProgram,
                    Value = string.Join(", ", dealerInfo.ProductInfo.Services.Select(s => s.Equipment?.Type))
                });
            }
            else
            {
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.ProductsForFinancingProgram,
                    Value = BlankValue
                });
            }

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.OemName,
                Value = !string.IsNullOrEmpty(dealerInfo.ProductInfo?.OemName) ? dealerInfo.ProductInfo.OemName : BlankValue
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.FinanceProviderName,
                Value = !string.IsNullOrEmpty(dealerInfo.ProductInfo?.FinanceProviderName) ? dealerInfo.ProductInfo.FinanceProviderName : BlankValue
            });

            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MonthlyCapitalValue,
                Value = dealerInfo.ProductInfo?.MonthlyFinancedValue != null ? dealerInfo.ProductInfo.MonthlyFinancedValue?.ToString(CultureInfo.InvariantCulture) : BlankValue
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MonthlyDealsToBeDeferred,
                Value = dealerInfo.ProductInfo?.PercentMonthlyDealsDeferred != null ? $"{dealerInfo.ProductInfo.PercentMonthlyDealsDeferred?.ToString(CultureInfo.InvariantCulture)}%" : $"{0.0M.ToString(CultureInfo.InvariantCulture)}%"
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.MarketingConsent,
                Value = dealerInfo.MarketingConsent ? "Y" : "N"
            });
            udfList.Add(new UDF()
            {
                Name = AspireUdfFields.CreditCheckConsent,
                Value = dealerInfo.CreditCheckConsent ? "Y" : "N"
            });

            if (dealerInfo?.Owners?.Any() == true)
            {
                udfList.AddRange(GetCompanyOwnersUdfs(dealerInfo));
            }

            if (!string.IsNullOrEmpty(dealerInfo.AccessKey))
            {
                var draftLink = _configuration.GetSetting(WebConfigKeys.DEALER_PORTAL_DRAFTURL_KEY) + dealerInfo.AccessKey;
                udfList.Add(new UDF()
                {
                    Name = AspireUdfFields.SubmissionUrl,
                    Value = draftLink
                });
            }

            //var leadSource = _configuration.GetSetting(WebConfigKeys.ONBOARDING_LEAD_SOURCE_KEY);
            //if (!string.IsNullOrEmpty(leadSource))
            //{
            //    udfList.Add(new UDF()
            //    {
            //        Name = AspireUdfFields.LeadSource,
            //        Value = leadSource
            //    });
            //}

            return udfList;
        }

        private string GetCompanyProvincesApproved(DealerInfo dealerInfo)
        {
            //probably will have more complex logic here, for include licences information
            //return string.Join(", ", dealerInfo.CompanyInfo.Provinces.Select(p => p.Province));
            var sb = new StringBuilder();
            var licenses = dealerInfo.AdditionalDocuments?.GroupBy(
                d => d.License?.LicenseDocuments?.FirstOrDefault()?.Province.Province);
            dealerInfo.CompanyInfo.Provinces.Select(p => p.Province).ForEach(p =>
            {
                var provLicenses = licenses?.FirstOrDefault(l => l.Key == p);
                if (provLicenses != null)
                {
                    var licInfo = string.Join("; ",
                        provLicenses.Select(
                            pl =>
                                $"License:{pl.License?.Name}, reg_number:{pl.Number}, expiry:{pl.ExpiredDate?.ToString("d", CultureInfo.CreateSpecificCulture("en-US")) ?? "no_expiry"}"));
                    sb.AppendLine($"{p}:{licInfo}");
                }
                else
                {
                    sb.AppendLine(p);
                }
            });
            return sb.ToString();
        }

        private IList<UDF> GetCompanyOwnersUdfs(DealerInfo dealerInfo)
        {
            const int maxOwners = 5;
            var ownerNum = 1;
            var owners = dealerInfo.Owners?.OrderBy(o => o.OwnerOrder).Take(maxOwners).ToList() ?? new List<OwnerInfo>();
            for (int i = owners.Count(); i < maxOwners; i++)
            {
                owners.Add(null);
            }
            
            var udfs = owners.SelectMany(owner =>
            {
                var ownerUdfs = new List<UDF>();
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerFirstName} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.FirstName) ? owner.FirstName : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerLastName} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.LastName) ? owner.LastName : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerDateOfBirth} {ownerNum}",
                    Value = owner?.DateOfBirth != null ? owner.DateOfBirth?.ToString("d", CultureInfo.CreateSpecificCulture("en-US")) : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerHomePhone} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.HomePhone) ? owner.HomePhone : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerMobilePhone} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.MobilePhone) ? owner.MobilePhone : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerEmail} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.EmailAddress) ? owner.EmailAddress : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerAddress} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.Address?.Street) ? owner.Address.Street : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerAddressCity} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.Address?.City) ? owner.Address.City : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerAddressPostalCode} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.Address?.PostalCode) ? owner.Address.PostalCode : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerAddressState} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.Address?.State) ? owner.Address.State : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerAddressUnit} {ownerNum}",
                    Value = !string.IsNullOrEmpty(owner?.Address?.Unit) ? owner.Address.Unit : BlankValue
                });
                ownerUdfs.Add(new UDF()
                {
                    Name = $"{AspireUdfFields.OwnerPercentageOfOwnership} {ownerNum}",
                    Value = owner?.PercentOwnership != null ? $"{owner.PercentOwnership?.ToString(CultureInfo.InvariantCulture)}%" : $"{0.0M.ToString(CultureInfo.InvariantCulture)}%"
                });               

                ownerNum++;
                return ownerUdfs;
            }).ToList();

            return udfs;
        }

        #endregion
    }
}
