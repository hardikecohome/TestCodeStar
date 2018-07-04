using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Helpers;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services.Signature;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Api.Models.Storage;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Logging;
using Unity.Interception.Utilities;
using FormField = DealnetPortal.Api.Models.Signature.FormField;

namespace DealnetPortal.Api.Integration.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ISignatureEngine _signatureEngine;
        private readonly IPdfEngine _pdfEngine;
        private readonly IContractRepository _contractRepository;
        private readonly ILoggingService _loggingService;
        private readonly IFileRepository _fileRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAspireService _aspireService;
        private readonly IAspireStorageReader _aspireStorageReader;               
        private readonly IMailService _mailService;               

        public DocumentService(
            ISignatureEngine signatureEngine, 
            IPdfEngine pdfEngine,
            IContractRepository contractRepository,
            IFileRepository fileRepository, 
            IUnitOfWork unitOfWork, 
            IAspireService aspireService,
            IAspireStorageReader aspireStorageReader,
            ILoggingService loggingService, 
            IDealerRepository dealerRepository, 
            IMailService mailService)
        {
            _signatureEngine = signatureEngine;
            _pdfEngine = pdfEngine;
            _contractRepository = contractRepository;
            _loggingService = loggingService;
            _dealerRepository = dealerRepository;
            _mailService = mailService;
            _fileRepository = fileRepository;
            _unitOfWork = unitOfWork;
            _aspireService = aspireService;
            _aspireStorageReader = aspireStorageReader;            
        }

        public async Task<Tuple<SignatureSummaryDTO, IList<Alert>>> StartSignatureProcess(int contractId, string ownerUserId,
            SignatureUser[] signatureUsers)
        {
            List<Alert> alerts = new List<Alert>();
            SignatureSummaryDTO summary = null;

            try
            {
                // Get contract
                var contract = _contractRepository.GetContract(contractId, ownerUserId);
                if (contract != null)
                {
                    _loggingService.LogInfo($"Started eSignature processing for contract [{contractId}]");

                    signatureUsers = PrepareSignatureUsers(contract, signatureUsers);

                    _signatureEngine.TransactionId = contract.Details?.SignatureTransactionId;
                    _signatureEngine.DocumentId = contract.Details?.SignatureDocumentId;                    

                    var agrRes = SelectAgreementTemplate(contract, ownerUserId);
                    if (agrRes?.Item2?.Any() == true)
                    {
                        alerts.AddRange(agrRes.Item2);
                    }

                    if (agrRes?.Item1 != null && alerts.All(a => a.Type != AlertType.Error))
                    {
                        var agreementTemplate = agrRes.Item1;

                        var trRes = await _signatureEngine.InitiateTransaction(contract, agreementTemplate);

                        if (trRes?.Any() ?? false)
                        {
                            alerts.AddRange(trRes);
                        }

                        var templateFields = await _signatureEngine.GetFormfFields();

                        var fields = PrepareFormFields(contract, templateFields?.Item1, ownerUserId);
                        _loggingService.LogInfo($"{fields.Count} fields collected");

                        var insertRes = await _signatureEngine.InsertDocumentFields(fields);

                        if (insertRes?.Any() ?? false)
                        {
                            alerts.AddRange(insertRes);
                        }                        

                        insertRes = await _signatureEngine.InsertSignatures(signatureUsers);
                        if (insertRes?.Any() ?? false)
                        {
                            alerts.AddRange(insertRes);
                        }
                        if (insertRes?.Any(a => a.Type == AlertType.Error) ?? false)
                        {
                            _loggingService.LogWarning(
                                $"Signature fields inserted into agreement document form with errors");
                        }
                        else
                        {
                            _loggingService.LogInfo(
                                $"Signature fields inserted into agreement document form successefully");
                        }

                        insertRes = await _signatureEngine.SubmitDocument(signatureUsers);

                        if (insertRes?.Any() ?? false)
                        {
                            alerts.AddRange(insertRes);
                        }
                        if (insertRes?.All(a => a.Type != AlertType.Error) == true)
                        {

                            UpdateContractDetails(contractId, ownerUserId, _signatureEngine.TransactionId,
                                _signatureEngine.DocumentId, null);
                            UpdateSignersInfo(contractId, ownerUserId, signatureUsers);
                            _loggingService.LogInfo(
                                $"Invitations for agreement document form sent successefully. TransactionId: [{_signatureEngine.TransactionId}], DocumentID [{_signatureEngine.DocumentId}]");
                            await UpdateContractStatus(contractId, ownerUserId);
                            summary = AutoMapper.Mapper.Map<SignatureSummaryDTO>(contract);
                        }
                    }
                }
                else
                {
                    var errorMsg = $"Can't get contract [{contractId}] for processing";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch(Exception ex)
            {
                var msg = $"Failed to initiate a digital signature for contract [{contractId}]";
                _loggingService.LogError(msg, ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = $"{msg}:{ex.ToString()}"
                });
            }

            LogAlerts(alerts);
            return new Tuple<SignatureSummaryDTO, IList<Alert>>(summary, alerts);
        }

        public async Task<Tuple<bool, IList<Alert>>> CheckPrintAgreementAvailable(int contractId, int documentTypeId,
            string ownerUserId)
        {
            bool isAvailable = false;
            List<Alert> alerts = new List<Alert>();

            // Get contract
            var contract = _contractRepository.GetContractAsUntracked(contractId, ownerUserId);
            if (contract != null)
            {
                //Check is pdf template in db. In this case we insert fields on place
                var agrRes = documentTypeId != (int) DocumentTemplateType.SignedInstallationCertificate
                    ? SelectAgreementTemplate(contract, ownerUserId)
                    : SelectInstallCertificateTemplate(contract, ownerUserId);
                if (agrRes?.Item1?.TemplateDocument?.TemplateBinary != null)
                {
                    isAvailable = true;
                }
                else
                {
                    //else we try to get contract from eSignature agreement template
                    //check is agreement created
                    if (!string.IsNullOrEmpty(contract.Details.SignatureTransactionId))
                    {
                        isAvailable = true;
                    }
                    else
                    {
                        // create draft agreement
                        var createRes = await StartSignatureProcess(contractId, ownerUserId, null).ConfigureAwait(false);
                        isAvailable = true;
                        if (createRes?.Item2?.Any() == true)
                        {
                            alerts.AddRange(createRes.Item2);
                            if (createRes.Item2.Any(a => a.Type == AlertType.Error))
                            {
                                isAvailable = false;
                            }
                        }
                    }
                }
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }
            LogAlerts(alerts);

            return new Tuple<bool, IList<Alert>>(isAvailable, alerts);
        }

        public async Task<Tuple<AgreementDocument, IList<Alert>>> GetPrintAgreement(int contractId, string ownerUserId)
        {
            List<Alert> alerts = new List<Alert>();
            AgreementDocument document = null;

            // Get contract
            var contract = _contractRepository.GetContractAsUntracked(contractId, ownerUserId);
            if (contract != null)
            {
                //Check is pdf template in db. In this case we insert fields on place
                var agrRes = SelectAgreementTemplate(contract, ownerUserId);

                if (agrRes?.Item1?.TemplateDocument?.TemplateBinary != null)
                {
                    MemoryStream ms = new MemoryStream(agrRes.Item1.TemplateDocument.TemplateBinary, true);

                    var formFields = _pdfEngine.GetFormfFields(ms);

                    var fields = PrepareFormFields(contract, formFields?.Item1, ownerUserId);
                    var insertRes = _pdfEngine.InsertFormFields(ms, fields);

                    if (insertRes?.Item2?.Any() ?? false)
                    {
                        alerts.AddRange(insertRes.Item2);
                    }

                    if (insertRes?.Item1 != null)
                    {
                        var buf = new byte[insertRes.Item1.Length];
                        await insertRes.Item1.ReadAsync(buf, 0, (int) insertRes.Item1.Length);
                        document = new AgreementDocument()
                        {
                            DocumentRaw = buf,
                            Name = agrRes.Item1.TemplateDocument.TemplateName
                        };

                        ReformatTempalteNameWithId(document, contract.Details.TransactionId);
                    }
                }
                else
                {
                    //try to get doc from eSignature
                    if (string.IsNullOrEmpty(contract.Details?.SignatureTransactionId))
                    {
                        // create draft agreement
                        var createRes = await StartSignatureProcess(contractId, ownerUserId, null).ConfigureAwait(false);
                        if (createRes?.Item2?.Any() == true)
                        {
                            alerts.AddRange(createRes.Item2);
                        }
                    }
                    if (alerts.All(a => a.Type != AlertType.Error))
                    {
                        var signedDoc = await GetSignedAgreement(contractId, ownerUserId).ConfigureAwait(false);
                        document = signedDoc?.Item1;
                        if (signedDoc?.Item2?.Any() == true)
                        {
                            alerts.AddRange(signedDoc.Item2);
                        }
                    }
                }
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }
            LogAlerts(alerts);

            return new Tuple<AgreementDocument, IList<Alert>>(document, alerts);
        }


        public async Task<Tuple<AgreementDocument, IList<Alert>>> GetSignedAgreement(int contractId, string ownerUserId)
        {
            List<Alert> alerts = new List<Alert>();
            AgreementDocument document = null;

            // Get contract
            var contract = _contractRepository.GetContractAsUntracked(contractId, ownerUserId);
            if (contract != null)
            {
                //check is agreement created
                if (!string.IsNullOrEmpty(contract.Details.SignatureTransactionId))
                {                    
                    _signatureEngine.TransactionId = contract.Details.SignatureTransactionId;
                    _signatureEngine.DocumentId = contract.Details.SignatureDocumentId;
                }
                else
                {
                    var errorMsg = $"Can't get DocuSign data for [{contractId}].";
                    alerts.Add(new Alert()
                    {
                        Code = ErrorCodes.CantGetContractFromDb,
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }

                var docResult = await _signatureEngine.GetDocument().ConfigureAwait(false);
                document = docResult.Item1;

                ReformatTempalteNameWithId(document, contract.Details?.TransactionId);

                if (docResult.Item2.Any())
                {
                    alerts.AddRange(docResult.Item2);
                }
                LogAlerts(alerts);
                return new Tuple<AgreementDocument, IList<Alert>>(document, alerts);
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }

            return new Tuple<AgreementDocument, IList<Alert>>(document, alerts);
        }

        public async Task<Tuple<AgreementDocument, IList<Alert>>> GetInstallCertificate(int contractId,
            string ownerUserId)
        {
            List<Alert> alerts = new List<Alert>();
            AgreementDocument document = null;

            // Get contract
            var contract = _contractRepository.GetContractAsUntracked(contractId, ownerUserId);
            if (contract != null)
            {
                //Check is pdf template in db. In this case we insert fields on place
                var agrRes = SelectInstallCertificateTemplate(contract, ownerUserId);
                if (agrRes?.Item1?.TemplateDocument?.TemplateBinary != null)
                {
                    MemoryStream ms = new MemoryStream(agrRes.Item1.TemplateDocument.TemplateBinary, true);

                    var templateFields = _pdfEngine.GetFormfFields(ms);

                    var fields = new List<FormField>();
                    FillHomeOwnerFields(fields, templateFields?.Item1, contract);
                    FillApplicantsFields(fields, contract);
                    FillEquipmentFields(fields, templateFields?.Item1, contract, ownerUserId);
                    FillDealerFields(fields, contract);
                    FillInstallCertificateFields(fields, contract);

                    var insertRes = _pdfEngine.InsertFormFields(ms, fields);
                    if (insertRes?.Item2?.Any() ?? false)
                    {
                        alerts.AddRange(insertRes.Item2);
                    }
                    if (insertRes?.Item1 != null)
                    {
                        var buf = new byte[insertRes.Item1.Length];
                        await insertRes.Item1.ReadAsync(buf, 0, (int) insertRes.Item1.Length);
                        document = new AgreementDocument()
                        {
                            DocumentRaw = buf,
                            Name = agrRes.Item1.TemplateDocument.TemplateName
                        };

                        ReformatTempalteNameWithId(document, contract.Details?.TransactionId);
                    }
                }
                else
                {
                    var errorMsg = $"Cannot find installation certificate template for contract [{contractId}]";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Message = errorMsg
                    });
                }
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
            }
            LogAlerts(alerts);
            return new Tuple<AgreementDocument, IList<Alert>>(document, alerts);
        }        

        public async Task<IList<Alert>> SyncSignatureStatus(int contractId, string ownerUserId)
        {
            var alerts = new List<Alert>();

            var contract = _contractRepository.GetContract(contractId, ownerUserId);
            if (contract != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(contract.Details?.SignatureTransactionId) && contract.Signers?.Any() == false)
                    {
                        var sUsers = PrepareSignatureUsers(contract, null);
                        UpdateSignersInfo(contract.Id, contract.DealerId, sUsers);
                    }
                    await UpdateContractStatus(contractId, ownerUserId);
                }
                catch (Exception e)
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Message = e.ToString()
                    });
                }
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.CantGetContractFromDb,
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }
            LogAlerts(alerts);
            return alerts;
        }        

        public async Task<Tuple<SignatureSummaryDTO, IList<Alert>>> CancelSignatureProcess(int contractId, string ownerUserId, string cancelReason = null)
        {
            List<Alert> alerts = new List<Alert>();
            SignatureSummaryDTO summary = null;

            // Get contract
            var contract = _contractRepository.GetContract(contractId, ownerUserId);
            if (contract != null)
            {
                _loggingService.LogInfo($"Started cancelling eSignature for contract [{contractId}]");                    
                _signatureEngine.TransactionId = contract.Details?.SignatureTransactionId;
                _signatureEngine.DocumentId = contract.Details?.SignatureDocumentId;

                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    var cancelRes = await _signatureEngine.CancelSignature(cancelReason).ConfigureAwait(false);
                    alerts.AddRange(cancelRes);
                }
                if (alerts.All(a => a.Type != AlertType.Error))
                {
                    _loggingService.LogInfo($"eSignature envelope {contract?.Details?.SignatureTransactionId} canceled for contract [{contractId}]");
                }
                else
                {
                    LogAlerts(alerts);
                }
                await UpdateContractStatus(contractId, ownerUserId);
                summary = AutoMapper.Mapper.Map<SignatureSummaryDTO>(contract);
            }
            else
            {
                var errorMsg = $"Can't get contract [{contractId}] for processing";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }

            return new Tuple<SignatureSummaryDTO, IList<Alert>>(summary, alerts);
        }

        public void CleanSignatureInfo(int contractId, string ownerUserId)
        {
            var contract = _contractRepository.GetContract(contractId, ownerUserId);
            if (contract != null)
            {
                try
                {
                    bool updated = CleanContractSignatureInfo(contract);
                    if (updated)
                    {
                        _unitOfWork.Save();
                    }
                }
                catch (Exception e)
                {
                    _loggingService.LogError("Error during processing of CleanSignatureInfo", e);
                }
            }
        }

        public async Task<IList<Alert>> UpdateSignatureUsers(int contractId, string ownerUserId, SignatureUser[] signatureUsers)
        {
            List<Alert> alerts = new List<Alert>();

            try
            {
                // Get contract
                var contract = _contractRepository.GetContract(contractId, ownerUserId);
                if (!string.IsNullOrEmpty(contract?.Details?.SignatureTransactionId) && signatureUsers?.Any() == true)
                {
                    _signatureEngine.TransactionId = contract.Details?.SignatureTransactionId;
                    _signatureEngine.DocumentId = contract.Details?.SignatureDocumentId;

                    signatureUsers = PrepareSignatureUsers(contract, signatureUsers);
                    UpdateSignersInfo(contractId, ownerUserId, signatureUsers, true);

                    signatureUsers = AutoMapper.Mapper.Map<SignatureUser[]>(contract.Signers.ToArray());

                    var updateAlerts = await _signatureEngine.UpdateSigners(signatureUsers);
                    if (updateAlerts?.Any() == true)
                    {
                        LogAlerts(updateAlerts);
                        alerts.AddRange(updateAlerts);
                    }
                }
                else
                {
                    var errorMsg = $"Can't get contract [{contractId}] for processing";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Code = ErrorCodes.CantGetContractFromDb,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                var msg = $"Failed to update signature users for contract [{contractId}]";
                _loggingService.LogError(msg, ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = $"{msg}:{ex.ToString()}"
                });
            }            
            return alerts;
        }
        public async Task<IList<Alert>> ProcessSignatureEvent(string notificationMsg)
        {
            var alerts = new List<Alert>();
            try
            {
                XDocument xDocument = XDocument.Parse(notificationMsg);
                var xmlns = xDocument?.Root?.Attribute(XName.Get("xmlns"))?.Value ?? "http://www.docusign.net/API/3.0";

                var envelopeStatusSection = xDocument?.Root?.Element(XName.Get("EnvelopeStatus", xmlns));
                var envelopeId = envelopeStatusSection?.Element(XName.Get("EnvelopeID", xmlns))?.Value;
                var timeZone = xDocument?.Root?.Element(XName.Get("TimeZone", xmlns))?.Value;

                var contract = _contractRepository.FindContractBySignatureId(envelopeId);
                if (contract != null && envelopeStatusSection != null)
                {
                    //check for signer users info exist in this contract
                    if (contract.Signers?.Any() != true)
                    {
                        var sUsers = PrepareSignatureUsers(contract, null);
                        UpdateSignersInfo(contract.Id, contract.DealerId, sUsers);
                    }

                    bool updated = false;
                    var oldStatus = contract.Details?.SignatureStatus;

                    if (timeZone != null)
                    {
                        
                        var updatedRes = await _signatureEngine.ParseStatusEvent(notificationMsg, contract);
                        if (updatedRes?.Item2?.Any() == true)
                        {
                            alerts.AddRange(updatedRes.Item2);
                        }
                        updated = updatedRes?.Item1 == true;                        
                    }
                    else
                    {
                        _loggingService.LogInfo($"Cannot find timezone info in the signature event for envelope {envelopeId}. Trying to request DocuSign");
                        await _signatureEngine.ServiceLogin().ConfigureAwait(false);
                        var updateStatusRes = await _signatureEngine.UpdateContractStatus(contract);
                        updated = updateStatusRes?.Item1 == true;                                                                                
                    }

                    if (updated)
                    {
                        _unitOfWork.Save();
                        if (oldStatus != contract.Details?.SignatureStatus)
                        {
                            await OnSignatureStatusChanged(contract.Id, contract.DealerId);
                        }
                    }
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Code = ErrorCodes.CantGetContractFromDb,
                        Header = "Cannot find contract",
                        Message = $"Cannot find contract for signature envelopeId {envelopeId}"
                    });
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,                   
                    Header = "Cannot parse DocuSign notification",
                    Message = $"Error occurred during parsing request from DocuSign: {ex.ToString()}"
                });
            }
            LogAlerts(alerts);
            return await Task.FromResult(alerts);
        }

        #region private  

        private async Task OnSignatureStatusChanged(int contractId, string contractOwnerId)
        {
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                try
                {                
                    bool updated = false;
                    switch (contract.Details.SignatureStatus)
                    {
                        case SignatureStatus.Completed:
                            //upload doc from docuSign to Aspire
                            updated |= await TransferSignedContractAgreement(contract).ConfigureAwait(false);
                            break;
                        case SignatureStatus.Deleted:
                            updated |= CleanContractSignatureInfo(contract);
                            break;
                        case SignatureStatus.Declined:
                            var dealerProvince = _aspireStorageReader.GetDealerInfo(contract.Dealer.AspireLogin).State;
                            await _mailService.SendDeclineToSign(contract, dealerProvince);
                            break;
                    }
                    if (updated)
                    {
                        _unitOfWork.Save();
                    }
                }
                catch (Exception e)
                {
                    _loggingService.LogError("Error during processing of OnSignatureStatusChanged", e);
                }
            }
        }

        private SignatureUser[] PrepareSignatureUsers(Contract contract, SignatureUser[] signatureUsers)
        {
            List<SignatureUser> usersForProcessing = new List<SignatureUser>();            

            var homeOwner = signatureUsers?.FirstOrDefault(u => u.Role == SignatureRole.HomeOwner)
                                ?? new SignatureUser() { Role = SignatureRole.HomeOwner };
            homeOwner.FirstName = !string.IsNullOrEmpty(homeOwner.FirstName) ? homeOwner.FirstName : contract?.PrimaryCustomer?.FirstName;
            homeOwner.LastName = !string.IsNullOrEmpty(homeOwner.LastName) ? homeOwner.LastName : contract?.PrimaryCustomer?.LastName;
            homeOwner.CustomerId = homeOwner.CustomerId ?? contract?.PrimaryCustomer?.Id;
            homeOwner.EmailAddress = !string.IsNullOrEmpty(homeOwner.EmailAddress)
                ? homeOwner.EmailAddress
                : contract?.Signers?.FirstOrDefault(s => s.SignerType == SignatureRole.HomeOwner)?.EmailAddress 
                    ?? contract?.PrimaryCustomer?.Emails.FirstOrDefault()?.EmailAddress;
            usersForProcessing.Add(homeOwner);

            var secondaryCustomers = contract?.SecondaryCustomers?.Where(sc => sc.IsDeleted != true);

            secondaryCustomers?.ForEach(cc =>
            {
                var su = signatureUsers?.FirstOrDefault(u => u.Role == SignatureRole.AdditionalApplicant &&
                                                   (cc.Id == u.CustomerId) ||
                                                   (cc.FirstName == u.FirstName && cc.LastName == u.LastName));
                su = su ?? signatureUsers?.FirstOrDefault(u =>
                         u.Role == SignatureRole.AdditionalApplicant && !u.CustomerId.HasValue && string.IsNullOrEmpty(u.FirstName) && string.IsNullOrEmpty(u.LastName));
                var coBorrower = su ?? new SignatureUser() { Role = SignatureRole.AdditionalApplicant };
                coBorrower.FirstName = !string.IsNullOrEmpty(coBorrower.FirstName) ? coBorrower.FirstName : cc.FirstName;
                coBorrower.LastName = !string.IsNullOrEmpty(coBorrower.LastName) ? coBorrower.LastName : cc.LastName;
                coBorrower.CustomerId = coBorrower.CustomerId ?? cc.Id;
                coBorrower.EmailAddress = !string.IsNullOrEmpty(coBorrower.EmailAddress)
                    ? coBorrower.EmailAddress
                    : contract.Signers?.FirstOrDefault(u => u.SignerType == SignatureRole.AdditionalApplicant &&
                                                            (cc.Id == u.Id) ||
                                                            (cc.FirstName == u.FirstName && cc.LastName == u.LastName))?.EmailAddress ?? cc.Emails.FirstOrDefault()?.EmailAddress;
                usersForProcessing.Add(coBorrower);
            });

            var dealerUser = signatureUsers?.FirstOrDefault(u => u.Role == SignatureRole.Dealer) ?? new SignatureUser() {Role = SignatureRole.Dealer};

            if (string.IsNullOrEmpty(dealerUser.LastName) || string.IsNullOrEmpty(dealerUser.EmailAddress))
            {
                var dealer = contract?.Signers?.FirstOrDefault(s => s.SignerType == SignatureRole.Dealer);
                dealerUser.FirstName = dealerUser.FirstName ?? dealer?.FirstName;
                dealerUser.LastName = dealerUser.LastName ?? dealer?.LastName ?? contract.Equipment?.SalesRep ?? contract.Dealer?.UserName;
                dealerUser.EmailAddress = dealerUser.EmailAddress ?? dealer?.EmailAddress ?? contract.Dealer?.Email;
            }
            usersForProcessing.Add(dealerUser);
            return usersForProcessing.ToArray();
        }

        private void UpdateSignersInfo(int contractId, string ownerUserId, SignatureUser[] signatureUsers, bool syncOnly = false)
        {
            try
            {
                var signers = AutoMapper.Mapper.Map<ContractSigner[]>(signatureUsers);
                if (signers?.Any() == true)
                {
                    _contractRepository.UpdateContractSigners(contractId, signers, ownerUserId, syncOnly);
                    _unitOfWork.Save();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error on update signers for contract {contractId}", ex);
            }            
        }

        private void UpdateContractDetails(int contractId, string ownerUserId, string transactionId, string dpId,
            SignatureStatus? status)
        {
            try
            {
                var contract = _contractRepository.GetContract(contractId, ownerUserId);
                if (contract != null)
                {
                    bool updated = false;
                    if (!string.IsNullOrEmpty(transactionId))
                    {
                        contract.Details.SignatureTransactionId = transactionId;
                        updated = true;
                    }

                    if (!string.IsNullOrEmpty(dpId))
                    {
                        contract.Details.SignatureDocumentId = dpId;
                        updated = true;
                    }

                    if (status.HasValue)
                    {
                        contract.Details.SignatureStatus = status.Value;
                        updated = true;
                    }

                    if (updated)
                    {
                        contract.Details.SignatureInitiatedTime = DateTime.UtcNow;
                        contract.Details.SignatureLastUpdateTime = DateTime.UtcNow;
                        _unitOfWork.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error on update contract details", ex);
            }
        }

        private async Task UpdateContractStatus(int contractId, string ownerUserId)
        {
            var contract = _contractRepository.GetContract(contractId, ownerUserId);
            if (contract != null)
            {
                var oldStatus = contract.Details.SignatureStatus;
                var updateStatusRes = await _signatureEngine.UpdateContractStatus(contract);
                if (updateStatusRes?.Item1 == true)
                {
                    _unitOfWork.Save();
                    if (oldStatus != contract.Details.SignatureStatus)
                    {
                        await OnSignatureStatusChanged(contractId, ownerUserId);
                    }
                }                
            }
        }

        private bool CleanContractSignatureInfo(Contract contract)
        {
            bool updated = false;

            if (contract.Details.SignatureStatus.HasValue)
            {
                contract.Details.SignatureStatus = null;
                updated = true;
            }
            if (!string.IsNullOrEmpty(contract.Details.SignatureStatusQualifier))
            {
                contract.Details.SignatureStatusQualifier = null;
                updated = true;
            }
            if (!string.IsNullOrEmpty(contract.Details.SignatureTransactionId))
            {
                contract.Details.SignatureTransactionId = null;
                updated = true;
            }
            if (contract.Details.SignatureInitiatedTime.HasValue)
            {
                contract.Details.SignatureInitiatedTime = null;
                updated = true;
            }
            if (contract.Details.SignatureLastUpdateTime.HasValue)
            {
                contract.Details.SignatureLastUpdateTime = null;
                updated = true;
            }            
            if (contract.Signers?.Any() == true)
            {
                contract.Signers.ForEach(s =>
                {
                    s.SignatureStatus = null;
                    s.SignatureStatusQualifier = null;
                    s.StatusLastUpdateTime = null;
                    s.Comment = null;
                    s.FirstName = null;
                    s.LastName = null;
                    updated |= true;
                });
            }

            return updated;
        }

        private async Task<bool> TransferSignedContractAgreement(Contract contract)
        {
            bool updated = false;
            try
            {            
                _loggingService.LogInfo($"Starting download signed contract for {contract.Id} from DocuSign");

                _signatureEngine.TransactionId = contract.Details.SignatureTransactionId;
                var logRes = await _signatureEngine.ServiceLogin().ConfigureAwait(false);
                var docResult = await _signatureEngine.GetDocument().ConfigureAwait(false);
                if (docResult?.Item1 != null)
                {
                    _loggingService.LogInfo(
                        $"Signer contract {docResult.Item1.Name} for contract {contract.Id} was downloaded from DocuSign");
                    ContractDocumentDTO document = new ContractDocumentDTO()
                    {
                        ContractId = contract.Id,
                        CreationDate = DateTime.Now,
                        DocumentTypeId = (int)DocumentTemplateType.SignedContract, // Signed contract !!
                        DocumentName = DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss", CultureInfo.InvariantCulture) + "_" + docResult.Item1.Name,
                        DocumentBytes = docResult.Item1.DocumentRaw
                    };
                    //DEAL-3306 - Digitally signed contract shouldn't be displayed in 'Paper Contract' tab on Dealer Portal
                    //_contractRepository.AddDocumentToContract(contract.Id, AutoMapper.Mapper.Map<ContractDocument>(document), contract.DealerId);
                    //updated = true;
                    var alerts = await _aspireService.UploadDocument(contract.Id, document, contract.DealerId).ConfigureAwait(false);
                    if (alerts?.All(a => a.Type != AlertType.Error) == true)
                    {
                        _loggingService.LogInfo(
                            $"Signer contract {docResult.Item1.Name} for contract {contract.Id} uploaded to Aspire successfully");                    
                    }
                    LogAlerts(alerts);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    $"Cannot transfer signed contract from DocuSign for contract {contract.Id} ", ex);
            }                        
            return updated;
        }        

        private void LogAlerts(IList<Alert> alerts)
        {
            alerts.ForEach(a =>
            {
                switch (a.Type)
                {
                    case AlertType.Error:
                        _loggingService.LogError(a.Message);
                        break;
                    case AlertType.Warning:
                        _loggingService.LogWarning(a.Message);
                        break;
                    case AlertType.Information:
                        _loggingService.LogInfo(a.Message);
                        break;
                }
            });
        }

        private List<FormField> PrepareFormFields(Contract contract, IList<FormField> templateFields, string ownerUserId)
        {
            var fields = new List<FormField>();

            FillHomeOwnerFields(fields, templateFields, contract);
            FillApplicantsFields(fields, contract);
            FillEquipmentFields(fields, templateFields, contract, ownerUserId);
            FillExistingEquipmentFields(fields, contract);
            FillPaymentFields(fields, contract);
            FillDealerFields(fields, contract);

            if (contract.Equipment?.HasExistingAgreements == true && _contractRepository.IsBill59Contract(contract.Id))
            {
                FillExistingAgreementsInfoFields(fields, contract);
            }

            return fields;
        }

        private Tuple<AgreementTemplate, IList<Alert>> SelectAgreementTemplate(Contract contract,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();
            var agreementType = contract.Equipment.AgreementType;
            var rentalProgram = contract.Equipment.RentalProgramType;
            var equipmentTypes = contract.Equipment.NewEquipment?.Select(ne => ne.Type).ToList() ?? new List<string>();
            var culture = CultureHelper.GetCurrentNeutralCulture();

            var province =
                contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)
                    ?.State?.ToProvinceCode();

            var dealerTemplates = _fileRepository.FindAgreementTemplates(at =>
                (at.DealerId == contractOwnerId) && (!at.DocumentTypeId.HasValue ||
                                                     at.DocumentTypeId == (int) DocumentTemplateType.SignedContract));

            if (!string.IsNullOrEmpty(contract.ExternalSubDealerName) || dealerTemplates?.Any() != true)
            {
                var extTemplates = _fileRepository.FindAgreementTemplates(at => !string.IsNullOrEmpty(at.ExternalDealerName) &&
                    (at.ExternalDealerName == contract.ExternalSubDealerName || at.ExternalDealerName == contract.Dealer.UserName) &&
                    (!at.DocumentTypeId.HasValue || at.DocumentTypeId == (int) DocumentTemplateType.SignedContract));
                if (extTemplates?.Any() == true && dealerTemplates == null)
                {
                    dealerTemplates = new List<AgreementTemplate>();
                    extTemplates.ForEach(et => dealerTemplates.Add(et));
                }                
            }

            if (!dealerTemplates?.Any() ?? true)
            {
                var parentDealerId = _dealerRepository.GetParentDealerId(contractOwnerId);
                if (!string.IsNullOrEmpty(parentDealerId))
                {
                    dealerTemplates = _fileRepository.FindAgreementTemplates(at =>
                        (at.DealerId == parentDealerId) && (!at.DocumentTypeId.HasValue ||
                                                            at.DocumentTypeId ==
                                                            (int) DocumentTemplateType.SignedContract));
                }
            }

            if (dealerTemplates?.Any() != true)
            {
                //otherwise select any common template
                var commonTemplates =
                    _fileRepository.FindAgreementTemplates(
                        at => string.IsNullOrEmpty(at.DealerId) && string.IsNullOrEmpty(at.ExternalDealerName) &&
                              (!at.DocumentTypeId.HasValue ||
                               at.DocumentTypeId == (int)DocumentTemplateType.SignedContract));
                dealerTemplates = commonTemplates;
            }

            // get agreement template 
            AgreementTemplate agreementTemplate = null;

            if (dealerTemplates?.Any() ?? false)
            {
                // if dealer has templates, select one
                var agreementTemplates = dealerTemplates.Where(at =>
                    ((agreementType == at.AgreementType) || (at.AgreementType.HasValue && at.AgreementType.Value.HasFlag(agreementType) && agreementType != AgreementType.LoanApplication))
                    && (string.IsNullOrEmpty(province) || (at.State?.Contains(province) ?? false))
                    && (agreementType == AgreementType.LoanApplication || at.AnnualEscalation == rentalProgram)
                    && (!equipmentTypes.Any() || (at.EquipmentType?.Split(' ', ',').Any(et => equipmentTypes.Contains(et)) ?? false))).ToList();

                if (!agreementTemplates.Any())
                {
                    agreementTemplates = dealerTemplates.Where(at =>
                        (!at.AgreementType.HasValue || (agreementType == at.AgreementType) || (at.AgreementType.Value.HasFlag(agreementType) && agreementType != AgreementType.LoanApplication))
                        && (string.IsNullOrEmpty(province) || (at.State?.Contains(province) ?? false))
                        && (agreementType == AgreementType.LoanApplication || at.AnnualEscalation == rentalProgram)
                        && (!equipmentTypes.Any() ||
                            (at.EquipmentType?.Split(' ', ',').Any(et => equipmentTypes.Contains(et)) ?? false))).ToList();
                }

                if (!agreementTemplates.Any())
                {
                    agreementTemplates = dealerTemplates.Where(at =>
                        ((agreementType == at.AgreementType) || (at.AgreementType.HasValue && at.AgreementType.Value.HasFlag(agreementType) && agreementType != AgreementType.LoanApplication))
                        && (agreementType == AgreementType.LoanApplication || at.AnnualEscalation == rentalProgram)
                        && (string.IsNullOrEmpty(province) || (at.State?.Contains(province) ?? false))).ToList();
                }

                if (!agreementTemplates.Any())
                {
                    agreementTemplates =
                        dealerTemplates.Where(at => ((agreementType == at.AgreementType) || (at.AgreementType.HasValue && at.AgreementType.Value.HasFlag(agreementType) && agreementType != AgreementType.LoanApplication))
                                                    && (agreementType == AgreementType.LoanApplication || at.AnnualEscalation == rentalProgram)
                                                            && string.IsNullOrEmpty(at.State) && string.IsNullOrEmpty(at.EquipmentType)).ToList();
                }

                if (agreementTemplates.Any())
                {
                    agreementTemplate = agreementTemplates.FirstOrDefault(at => at.Culture == culture) ??
                                        agreementTemplates.FirstOrDefault();
                }
            }            

            if (agreementTemplate == null)
            {
                var errorMsg =
                    $"Can't find agreement template for a dealer contract {contractOwnerId} with province = {province} and type = {agreementType}";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Can't find agreement template",
                    Message = errorMsg
                });
            }

            return new Tuple<AgreementTemplate, IList<Alert>>(agreementTemplate, alerts);
        }

        private Tuple<AgreementTemplate, IList<Alert>> SelectInstallCertificateTemplate(Contract contract,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();
            AgreementTemplate agreementTemplate = null;
            var province =
                contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)
                    ?.State?.ToProvinceCode();
            var dealerCertificates = _fileRepository.FindAgreementTemplates(at =>
                (at.DealerId == contractOwnerId) && (at.DocumentTypeId ==
                                                     (int) DocumentTemplateType.SignedInstallationCertificate));
            if (dealerCertificates?.Any() ?? false)
            {
                agreementTemplate = dealerCertificates.FirstOrDefault(
                                        cert => (contract.Equipment.AgreementType == cert.AgreementType) || (cert.AgreementType.HasValue && cert.AgreementType.Value.HasFlag(contract.Equipment.AgreementType) && contract.Equipment.AgreementType != AgreementType.LoanApplication))
                                    ?? dealerCertificates.FirstOrDefault();
            }
            else
            {
                var daeler = _contractRepository.GetDealer(contractOwnerId);
                if (daeler != null)
                {
                    var appCertificates = _fileRepository.FindAgreementTemplates(at =>
                        (at.ApplicationId == daeler.ApplicationId) &&
                        (at.DocumentTypeId == (int) DocumentTemplateType.SignedInstallationCertificate) && (string.IsNullOrEmpty(province) || (at.State.Contains(province))));
                    if (appCertificates?.Any() ?? false)
                    {
                        agreementTemplate = appCertificates.FirstOrDefault(
                                                cert => (contract.Equipment.AgreementType == cert.AgreementType) || (cert.AgreementType.HasValue && cert.AgreementType.Value.HasFlag(contract.Equipment.AgreementType) && contract.Equipment.AgreementType != AgreementType.LoanApplication))
                                            ?? appCertificates.FirstOrDefault();
                    }
                    else
                    {
                        appCertificates = _fileRepository.FindAgreementTemplates(at =>
                        (at.ApplicationId == daeler.ApplicationId) &&
                        (at.DocumentTypeId == (int)DocumentTemplateType.SignedInstallationCertificate));
                        if (appCertificates?.Any() ?? false)
                        {
                            agreementTemplate = appCertificates.FirstOrDefault(
                                                    cert => (contract.Equipment.AgreementType == cert.AgreementType) || (cert.AgreementType.HasValue && cert.AgreementType.Value.HasFlag(contract.Equipment.AgreementType) && contract.Equipment.AgreementType != AgreementType.LoanApplication))
                                                ?? appCertificates.FirstOrDefault();
                        }
                    }
                }
            }

            if (agreementTemplate == null)
            {
                var errorMsg =
                    $"Can't find installation certificate template for a dealer contract {contractOwnerId}";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Can't find installation certificate template",
                    Message = errorMsg
                });
            }

            return new Tuple<AgreementTemplate, IList<Alert>>(agreementTemplate, alerts);
        }

        private void FillHomeOwnerFields(List<FormField> formFields, IList<FormField> templateFields, Contract contract)
        {
            if (contract.Details.TransactionId != null)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ApplicationId,
                    Value = contract.Details.TransactionId
                });
            }
            if (contract.PrimaryCustomer != null)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.FirstName,
                    Value = contract.PrimaryCustomer.FirstName
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.LastName,
                    Value = contract.PrimaryCustomer.LastName
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.DateOfBirth,
                    Value = contract.PrimaryCustomer.DateOfBirth.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.DealerInitials,
                    Value = contract.PrimaryCustomer.DealerInitial
                });

                if (contract.PrimaryCustomer.VerificationIdName == "Driver’s license")
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.CustomerIdTypeDriverLicense,
                        Value = "true"
                    });                    
                }
                else if (contract.PrimaryCustomer.VerificationIdName != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.CustomerIdTypeOther,
                        Value = "true"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CustomerIdTypeOtherValue,
                        Value = contract.PrimaryCustomer.VerificationIdName
                    });
                }
                if (contract.PrimaryCustomer.Locations?.Any() ?? false)
                {
                    var mainAddress =
                        contract.PrimaryCustomer?.Locations?.FirstOrDefault(
                            l => l.AddressType == AddressType.MainAddress);
                    if (mainAddress != null)
                    {                        
                        if (!string.IsNullOrEmpty(mainAddress.Unit) && templateFields?.All(tf => tf.Name != PdfFormFields.SuiteNo) == true)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = PdfFormFields.InstallationAddress,
                                Value = $"{mainAddress.Street}, {Resources.Resources.Suite} {mainAddress.Unit}"
                            });
                        }
                        else
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = PdfFormFields.InstallationAddress,
                                Value = mainAddress.Street
                            });
                        }                        
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.City,
                            Value = mainAddress.City
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.Province,
                            Value = mainAddress.State
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.PostalCode,
                            Value = mainAddress.PostalCode
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.SuiteNo,
                            Value = mainAddress.Unit
                        });
                    }
                    var mailAddress =
                        contract.PrimaryCustomer?.Locations?.FirstOrDefault(
                            l => l.AddressType == AddressType.MailAddress);
                    if (mailAddress != null)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.CheckBox,
                            Name = PdfFormFields.IsMailingDifferent,
                            Value = "true"
                        });
                        var sMailAddress = !string.IsNullOrEmpty(mailAddress.Unit) ? 
                                        $"{mailAddress.Street}, {Resources.Resources.Suite} {mailAddress.Unit}, {mailAddress.City}, {mailAddress.State}, {mailAddress.PostalCode}" 
                                        : $"{mailAddress.Street}, {mailAddress.City}, {mailAddress.State}, {mailAddress.PostalCode}";
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.MailingAddress,
                            Value = sMailAddress
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.MailingOrPreviousAddress,
                            Value = sMailAddress
                        });
                    }
                    var previousAddress =
                        contract.PrimaryCustomer?.Locations?.FirstOrDefault(
                            l => l.AddressType == AddressType.PreviousAddress);
                    if (previousAddress != null)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.CheckBox,
                            Name = PdfFormFields.IsPreviousAddress,
                            Value = "true"
                        });
                        var sPrevAddress = !string.IsNullOrEmpty(previousAddress.Unit) ?
                            $"{previousAddress.Street}, {Resources.Resources.Suite} {previousAddress.Unit}, {previousAddress.City}, {previousAddress.State}, {previousAddress.PostalCode}"
                            : $"{previousAddress.Street}, {previousAddress.City}, {previousAddress.State}, {previousAddress.PostalCode}";

                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.PreviousAddress,
                            Value = sPrevAddress
                        });
                        if (mailAddress == null)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = PdfFormFields.MailingOrPreviousAddress,
                                Value = sPrevAddress
                            });
                        }
                    }
                    if (contract.HomeOwners?.Any(ho => ho.Id == contract.PrimaryCustomer.Id) ?? false)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.CheckBox,
                            Name = PdfFormFields.IsHomeOwner,
                            Value = "true"
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.CheckBox,
                            Name = $"{PdfFormFields.IsHomeOwner}_2",
                            Value = "true"
                        });
                    }
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CustomerName,
                        Value = $"{contract.PrimaryCustomer.LastName} {contract.PrimaryCustomer.FirstName}"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = $"{PdfFormFields.CustomerName}2",
                        Value = $"{contract.PrimaryCustomer.LastName} {contract.PrimaryCustomer.FirstName}"
                    });
                }
            }

            if (contract.PrimaryCustomer?.Emails?.Any() ?? false)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EmailAddress,
                    Value =
                        contract.PrimaryCustomer.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)
                            ?.EmailAddress ?? contract.PrimaryCustomer.Emails.First()?.EmailAddress
                });
            }

            if (contract.PrimaryCustomer?.Phones?.Any() ?? false)
            {
                var homePhone = contract.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Home);
                var cellPhone = contract.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell);
                var businessPhone =
                    contract.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Business);

                if (homePhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.HomePhone,
                        Value = homePhone.PhoneNum
                    });
                }
                if (cellPhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CellPhone,
                        Value = cellPhone.PhoneNum
                    });
                }
                if (businessPhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.BusinessPhone,
                        Value = businessPhone.PhoneNum
                    });
                }
                if (businessPhone != null || cellPhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.BusinessOrCellPhone,
                        Value = businessPhone?.PhoneNum ?? cellPhone?.PhoneNum
                    });
                }
                if (templateFields?.Any(tf => tf.Name == PdfFormFields.CellOrHomePhone) == true && (homePhone != null || cellPhone != null))
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CellOrHomePhone,
                        Value = cellPhone?.PhoneNum ?? homePhone?.PhoneNum
                    });
                }
            }

            if (!string.IsNullOrEmpty(contract.PrimaryCustomer?.Sin))
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.Sin,
                    Value = contract.PrimaryCustomer.Sin
                });
            }

            if (!string.IsNullOrEmpty(contract.PrimaryCustomer?.DriverLicenseNumber))
            {
                var dl = contract.PrimaryCustomer.DriverLicenseNumber.Replace(" ", "").Replace("-", "");
                for (int ch = 1;
                    ch <= Math.Min(dl.Length, 15);
                    ch++)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = $"{PdfFormFields.Dl}{ch}",
                        Value = $"{dl[ch - 1]}"
                    });
                }
            }

            if (contract.PrimaryCustomer.AllowCommunicate == true)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.CheckBox,
                    Name = PdfFormFields.AllowCommunicate,
                    Value = "true"
                });
            }
        }

        private void FillApplicantsFields(List<FormField> formFields, Contract contract)
        {
            if (contract.SecondaryCustomers?.Any(sc => sc.IsDeleted != true) ?? false)
            {
                var addApplicant = contract.SecondaryCustomers.First(sc => sc.IsDeleted != true);
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.FirstName2,
                    Value = addApplicant.FirstName
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.LastName2,
                    Value = addApplicant.LastName
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.DateOfBirth2,
                    Value = addApplicant.DateOfBirth.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = "ID2",
                    Value = addApplicant.DealerInitial
                });

                if (addApplicant.AllowCommunicate == true)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.AllowCommunicate2,
                        Value = "true"
                    });
                }

                if (addApplicant.VerificationIdName == "Driver’s license")
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = "Tiv2",
                        Value = "true"
                    });
                }
                else if (addApplicant.VerificationIdName != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = "Tiv2Other",
                        Value = "true"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = "OtherID_2",
                        Value = addApplicant.VerificationIdName
                    });
                }

                if (!string.IsNullOrEmpty(addApplicant.RelationshipToMainBorrower))
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.RelationshipToCustomer2,
                        Value = addApplicant.RelationshipToMainBorrower
                    });
                }

                var mainAddress2 =
                    addApplicant.Locations?.FirstOrDefault(
                        l => l.AddressType == AddressType.MainAddress) ?? contract.PrimaryCustomer.Locations?.FirstOrDefault(m => m.AddressType == AddressType.MainAddress);
                if (mainAddress2 != null)
                {
                    if (!string.IsNullOrEmpty(mainAddress2.Unit))//&&  templateFields?.All(tf => tf.Name != PdfFormFields.SuiteNo2) == true)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.InstallationAddress2,
                            Value = $"{mainAddress2.Street}, {Resources.Resources.Suite} {mainAddress2.Unit}"
                        });
                    }
                    else
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.InstallationAddress2,
                            Value = mainAddress2.Street
                        });
                    }
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.City2,
                        Value = mainAddress2.City
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.Province2,
                        Value = mainAddress2.State
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.PostalCode2,
                        Value = mainAddress2.PostalCode
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.SuiteNo2,
                        Value = mainAddress2.Unit
                    });
                }
                var mailAddress2 =
                    addApplicant.Locations?.FirstOrDefault(
                        l => l.AddressType == AddressType.MailAddress);
                if (mailAddress2 != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsMailingDifferent2,
                        Value = "true"
                    });
                    var sMailAddress = !string.IsNullOrEmpty(mailAddress2.Unit) ?
                                    $"{mailAddress2.Street}, {Resources.Resources.Suite} {mailAddress2.Unit}, {mailAddress2.City}, {mailAddress2.State}, {mailAddress2.PostalCode}"
                                    : $"{mailAddress2.Street}, {mailAddress2.City}, {mailAddress2.State}, {mailAddress2.PostalCode}";
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.MailingAddress2,
                        Value = sMailAddress
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.MailingOrPreviousAddress2,
                        Value = sMailAddress
                    });
                }
                var previousAddress =
                    addApplicant.Locations?.FirstOrDefault(
                        l => l.AddressType == AddressType.PreviousAddress);
                if (previousAddress != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsPreviousAddress2,
                        Value = "true"
                    });
                    var sPrevAddress = !string.IsNullOrEmpty(previousAddress.Unit) ?
                        $"{previousAddress.Street}, {Resources.Resources.Suite} {previousAddress.Unit}, {previousAddress.City}, {previousAddress.State}, {previousAddress.PostalCode}"
                        : $"{previousAddress.Street}, {previousAddress.City}, {previousAddress.State}, {previousAddress.PostalCode}";

                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.PreviousAddress2,
                        Value = sPrevAddress
                    });
                    if (mailAddress2 == null)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.MailingOrPreviousAddress2,
                            Value = sPrevAddress
                        });
                    }
                }
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EmailAddress2,
                    Value = addApplicant.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ??
                        addApplicant.Emails.First()?.EmailAddress
                });
                if (!string.IsNullOrEmpty(addApplicant?.Sin))
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.Sin2,
                        Value = addApplicant.Sin
                    });
                }
                var homePhone = addApplicant.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Home);
                var cellPhone = addApplicant.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Cell);
                if (homePhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.HomePhone2,
                        Value = homePhone.PhoneNum
                    });
                }
                if (cellPhone != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CellPhone2,
                        Value = cellPhone.PhoneNum
                    });
                }

                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.CustomerName2,
                    Value =
                        $"{addApplicant.LastName} {addApplicant.FirstName}"
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = $"{PdfFormFields.CustomerName2}2",
                    Value =
                        $"{addApplicant.LastName} {addApplicant.FirstName}"
                });

                if (contract.HomeOwners?.Any(ho => ho.Id == addApplicant.Id) ?? false)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsHomeOwner2,
                        Value = "true"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = $"{PdfFormFields.IsHomeOwner2}_2",
                        Value = "true"
                    });
                }                
            }
        }

        private void FillEquipmentFields(List<FormField> formFields, IList<FormField> templateFields, Contract contract, string ownerUserId)
        {
            if (contract.Equipment?.NewEquipment?.Where(ne => ne.IsDeleted != true).Any() ?? false)
            {
                var newEquipments = contract.Equipment.NewEquipment.Where(ne => ne.IsDeleted != true).ToList();
                var fstEq = newEquipments.First();                

                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EquipmentQuantity,
                    Value = newEquipments.Count(ne => ne.Type == fstEq.Type).ToString()
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EquipmentCost,
                    Value = newEquipments.Select(ne => ne.Cost).Sum().ToString()
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = $"{PdfFormFields.EquipmentCost}_2",
                    Value = newEquipments.Select(ne => ne.Cost).Sum().ToString()
                });

                if (fstEq != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.EquipmentDescription,
                        Value = fstEq.Description
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsEquipment,
                        Value = "true"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.EquipmentMonthlyRental,
                        Value = fstEq.MonthlyCost?.ToString("F", CultureInfo.InvariantCulture)
                    });
                }

                var othersEq = new List<NewEquipment>();
                foreach (var eq in newEquipments)
                {
                    var monthlyCost = eq.MonthlyCost?.ToString("F", CultureInfo.InvariantCulture);

                    switch (eq.Type)
                    {
                        case "ECO1": // Air Conditioner
                            // check is already filled
                            if (!formFields.Exists(f => f.Name == PdfFormFields.IsAirConditioner))
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = PdfFormFields.IsAirConditioner,
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.AirConditionerDetails,
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.AirConditionerMonthlyRental,
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                        case "ECO2": // Boiler
                            if (!formFields.Exists(f => f.Name == PdfFormFields.IsBoiler))
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = PdfFormFields.IsBoiler,
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.BoilerDetails,
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.BoilerMonthlyRental,
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                        case "ECO40": // Air Handler - we have common 
                            if (!formFields.Exists(f => f.Name == PdfFormFields.IsFurnace))
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = PdfFormFields.IsFurnace,
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.FurnaceDetails,
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.FurnaceMonthlyRental,
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                        case "ECO6": // HWT
                            if (!formFields.Exists(f => f.Name == PdfFormFields.IsWaterHeater))
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = PdfFormFields.IsWaterHeater,
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.WaterHeaterDetails,
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.WaterHeaterMonthlyRental,
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                        case "ECO7": // Plumbing
                            othersEq.Add(eq);
                            break;
                        case "ECO9": // Roofing
                            othersEq.Add(eq);
                            break;
                        case "ECO10": // Siding
                            othersEq.Add(eq);
                            break;
                        case "ECO11": // Tankless Water Heater
                            othersEq.Add(eq);
                            break;
                        case "ECO13": // Windows
                            othersEq.Add(eq);
                            break;
                        case "ECO23": // Air/Water Filtration
                            if (!formFields.Exists(f => f.Name == PdfFormFields.IsWaterFiltration))
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = PdfFormFields.IsWaterFiltration,
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.WaterFiltrationDetails,
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.WaterFiltrationMonthlyRental,
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                        case "ECO3": // Doors
                        case "ECO4": // Fireplace
                        case "ECO5": // Furnace                        
                        case "ECO38": // Sunrooms
                        case "ECO42": // Flooring
                        case "ECO43": // Porch Enclosure
                        case "ECO44": // Water Treatment System
                        case "ECO45": // Heat Pump
                        case "ECO46": // HRV
                        case "ECO47": // Bathroom
                        case "ECO48": // Kitchen
                        case "ECO49": // Hepa System
                        case "ECO50": // Unknown
                        case "ECO52": // Security System
                        case "ECO55": // Basement Repair
                        default:
                            // for all others
                            if (templateFields?.Any(tf => tf.Name == $"Is_{eq.Type}") == true && !formFields.Exists(f => f.Name == $"Is_{eq.Type}"))
                            {
                                //added logic for add fields in format:
                                //Is_ECO(X)
                                //EXO(X)_Details
                                //EXO(X)_MonthlyRental
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.CheckBox,
                                    Name = $"Is_{eq.Type}",
                                    Value = "true"
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = $"{eq.Type}_Details",
                                    Value = eq.Description
                                });
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = $"{eq.Type}_MonthlyRental",
                                    Value = monthlyCost
                                });
                            }
                            else
                            {
                                othersEq.Add(eq);
                            }
                            break;
                    }
                }
                if (othersEq.Any())
                {
                    for (int i = 0; i < othersEq.Count; i++)
                    {
                        var monthlyCost = othersEq[i].MonthlyCost?.ToString("F", CultureInfo.InvariantCulture);

                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.CheckBox,
                            Name = $"{PdfFormFields.IsOtherBase}{i + 1}",
                            Value = "true"
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = $"{PdfFormFields.OtherDetailsBase}{i + 1}",
                            Value = othersEq[i].Description
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = $"{PdfFormFields.OtherMonthlyRentalBase}{i + 1}",
                            Value = monthlyCost
                        });
                    }
                }
                for (int i = 0; i < newEquipments.Count; i++)
                {
                
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = $"{PdfFormFields.EquipmentQuantity}_{i}",
                        Value = "1"
                    });

                    var eqType = _contractRepository.GetEquipmentTypeInfo(newEquipments.ElementAt(i).Type);
                    string eqTypeDescr = null;
                    if (eqType != null)
                    {
                        eqTypeDescr = ResourceHelper.GetGlobalStringResource(eqType.DescriptionResource) ??
                                          eqType.Description;
                    }                    
                    var eqDescription = !string.IsNullOrEmpty(eqTypeDescr)
                        ? $"{eqTypeDescr} - {newEquipments.ElementAt(i).Description}" : newEquipments.ElementAt(i).Description;                    
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = $"{PdfFormFields.EquipmentDescription}_{i}",
                        Value = eqDescription
                    });                    
                }
                // support old contracts with EstimatedInstallationDate in Equipment
                var estimatedInstallationDate = contract.Equipment.EstimatedInstallationDate ??
                                                newEquipments.First()?.EstimatedInstallationDate;
                if (estimatedInstallationDate.HasValue)
                {
                    var cultureInfo = CultureInfo.GetCultureInfo(CultureHelper.GetCurrentCulture());
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.InstallDate,
                        Value = estimatedInstallationDate.Value.Hour > 0 ? estimatedInstallationDate.Value.ToString("g", cultureInfo)
                            : estimatedInstallationDate.Value.ToString("d", cultureInfo)                        
                    });
                }

                var totalRetailPrice = newEquipments.Where(ne => ne.EstimatedRetailCost.HasValue).Aggregate(0.0m,
                    (sum, ne) => sum + ne.EstimatedRetailCost.Value);
                if (totalRetailPrice > 0)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.TotalRetailPrice,
                        Value = totalRetailPrice.ToString("F", CultureInfo.InvariantCulture)
                    });
                }

                if (contract.Details.AgreementType != AgreementType.LoanApplication)
                {
                    FillTotalAmountPayable(formFields, contract, ownerUserId);
                }
            }
            if (contract.Equipment != null)
            {
                var paySummary = _contractRepository.GetContractPaymentsSummary(contract.Id);                

                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.TotalPayment,
                    Value = (contract.Equipment.AgreementType == AgreementType.LoanApplication) ?
                        paySummary.TotalAllMonthlyPayment?.ToString("F", CultureInfo.InvariantCulture) :
                        paySummary.TotalMonthlyPayment?.ToString("F", CultureInfo.InvariantCulture)
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.TotalMonthlyPayment,
                    Value = paySummary.MonthlyPayment?.ToString("F", CultureInfo.InvariantCulture)
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.MonthlyPayment,
                    Value = paySummary.LoanDetails?.TotalMCO.ToString("F", CultureInfo.InvariantCulture)
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.Hst,
                    Value = paySummary.Hst?.ToString("F", CultureInfo.InvariantCulture)
                });

                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.RequestedTerm,
                    Value = (contract.Equipment.AgreementType == AgreementType.LoanApplication)
                        ? contract.Equipment.LoanTerm?.ToString()
                        : contract.Equipment.RequestedTerm?.ToString()
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.AmortizationTerm,
                    Value = contract.Equipment.AmortizationTerm?.ToString()
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.DownPayment,
                    Value = contract.Equipment.DownPayment?.ToString("F", CultureInfo.InvariantCulture)
                });

                if (contract.Equipment.IsFeePaidByCutomer.HasValue && contract.Equipment.IsFeePaidByCutomer.Value)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.AdmeenFee,
                        Value = contract.Equipment.AdminFee?.ToString("F", CultureInfo.InvariantCulture)
                    });                    
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CustomerRate2,
                        Value = paySummary.LoanDetails?.AnnualPercentageRate.ToString("F", CultureInfo.InvariantCulture) ?? "0.0"
                    });
                }
                else
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.AdmeenFee,
                        Value = "n/a"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.CustomerRate2,
                        Value = contract.Equipment.CustomerRate?.ToString("F", CultureInfo.InvariantCulture)
                    });
                }
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.CustomerRate,
                    Value = contract.Equipment.CustomerRate?.ToString("F", CultureInfo.InvariantCulture)
                });

                if (contract.Equipment.AgreementType == AgreementType.LoanApplication &&
                    paySummary?.LoanDetails != null)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.LoanTotalCashPrice,
                        Value = paySummary.LoanDetails.LoanTotalCashPrice.ToString("F", CultureInfo.InvariantCulture)
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.LoanAmountFinanced,
                        Value = paySummary.LoanDetails.TotalAmountFinanced.ToString("F", CultureInfo.InvariantCulture)
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.LoanTotalObligation,
                        Value = paySummary.LoanDetails.TotalObligation.ToString("F", CultureInfo.InvariantCulture)
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.LoanBalanceOwing,
                        Value = paySummary.LoanDetails.ResidualBalance.ToString("F", CultureInfo.InvariantCulture)
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.LoanTotalBorowingCost,
                        Value = paySummary.LoanDetails.TotalBorowingCost.ToString("F", CultureInfo.InvariantCulture)
                    });                    
                }
                else
                {
                    //for rentals
                }

                if (contract.Equipment.DeferralType != DeferralType.NoDeferral)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.YesDeferral,
                        Value = "true"
                    });
                    var defTerm = contract.Equipment.DeferralType.GetPersistentEnumDescription().Split()[0];
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.DeferralTerm,
                        Value = defTerm
                    });
                }
                else
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.NoDeferral,
                        Value = "true"
                    });
                }
            }
        }

        private void FillTotalAmountPayable(List<FormField> formFields, Contract contract, string ownerUserId)
        {
            if (contract.Details.AgreementType != AgreementType.LoanApplication && contract.Equipment.RentalProgramType.HasValue)
            {
                var newEquipments = contract.Equipment.NewEquipment.Where(ne => ne.IsDeleted != true).ToList();
                var rate =
                    _contractRepository.GetProvinceTaxRate(
                        (contract.PrimaryCustomer?.Locations.FirstOrDefault(
                             l => l.AddressType == AddressType.MainAddress) ??
                         contract.PrimaryCustomer?.Locations.First())?.State.ToProvinceCode());

                decimal totalAmountUsefulLife = 0m;
                decimal totalAmountRentalLife = 0m;

                ////default factors for with escalation                
                var totalAmountFactor = 1.17313931606035m;
                var annualEscalation = 3.5 / 100;

                if (contract.Equipment.RentalProgramType == AnnualEscalationType.Escalation0)
                {
                    //without escalation
                    totalAmountFactor = 1.0m;
                    annualEscalation = 0.0;
                }

                totalAmountRentalLife = Math.Round(newEquipments.Sum(ne => ne.MonthlyCost ?? 0) * (1 + ((decimal?)rate?.Rate ?? 0.0m) / 100), 2);
                totalAmountRentalLife = Math.Round(totalAmountRentalLife * (contract.Equipment.RequestedTerm ?? 0) * totalAmountFactor, 2);

                totalAmountUsefulLife = newEquipments.Where(ne => ne.MonthlyCost.HasValue).Aggregate(0.0m,
                    (sum, ne) =>
                    {
                        var costWithHst =
                            Math.Round(ne.MonthlyCost.Value * (1 + ((decimal?)rate?.Rate ?? 0.0m) / 100), 2);
                        var priceAfter10 =
                            Math.Round(costWithHst * (decimal)Math.Pow(1 + annualEscalation, 9), 2);
                        var lifeOver10 =
                            ((_contractRepository.GetEquipmentTypeInfo(ne.Type)?.UsefulLife ?? 10) - 10) * 12;
                        var totalAfter10 = Math.Round(priceAfter10 * lifeOver10, 2);
                        return sum + totalAfter10;
                    });
                totalAmountUsefulLife += totalAmountRentalLife;

                if (totalAmountUsefulLife > 0)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.TotalAmountUsefulLife,
                        Value = totalAmountUsefulLife.ToString("F", CultureInfo.InvariantCulture)
                    });
                }
                if (totalAmountRentalLife > 0)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.TotalAmountRentalTerm,
                        Value = totalAmountRentalLife.ToString("F", CultureInfo.InvariantCulture)
                    });
                }
            }
        }

        private void FillPaymentFields(List<FormField> formFields, Contract contract)
        {
            if (contract.PaymentInfo != null)
            {
                if (contract.PaymentInfo.PaymentType == PaymentType.Enbridge)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsEnbridge,
                        Value = "true"
                    });
                    if (!string.IsNullOrEmpty(contract.PaymentInfo.EnbridgeGasDistributionAccount))
                    {
                        var ean = contract.PaymentInfo.EnbridgeGasDistributionAccount.Replace(" ", "");
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.EnbridgeAccountNumber,
                            Value = ean
                        });
                        for (int ch = 1;
                            ch <= Math.Min(ean.Length, 12);
                            ch++)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = $"{PdfFormFields.Ean}{ch}",
                                Value = $"{ean[ch - 1]}"
                            });
                        }
                    }
                }
                else
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.IsPAD,
                        Value = "true"
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = contract.PaymentInfo.PrefferedWithdrawalDate == WithdrawalDateType.First
                            ? PdfFormFields.IsPAD1
                            : PdfFormFields.IsPAD15,
                        Value = "true"
                    });

                    if (!string.IsNullOrEmpty(contract.PaymentInfo.BlankNumber))
                    {
                        var bn = contract.PaymentInfo.BlankNumber.Replace(" ", "");
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.BankNumber,
                            Value = bn
                        });
                        for (int ch = 1;
                            ch <= Math.Min(bn.Length, 3);
                            ch++)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = $"{PdfFormFields.Bn}{ch}",
                                Value = $"{bn[ch - 1]}"
                            });
                        }
                    }
                    if (!string.IsNullOrEmpty(contract.PaymentInfo.TransitNumber))
                    {
                        var tn = contract.PaymentInfo.TransitNumber.Replace(" ", "");
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.TransitNumber,
                            Value = tn
                        });
                        for (int ch = 1;
                            ch <= Math.Min(tn.Length, 5);
                            ch++)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = $"{PdfFormFields.Tn}{ch}",
                                Value = $"{tn[ch - 1]}"
                            });
                        }
                    }
                    if (!string.IsNullOrEmpty(contract.PaymentInfo.AccountNumber))
                    {
                        var an = contract.PaymentInfo.AccountNumber.Replace(" ", "");
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.AccountNumber,
                            Value = an
                        });
                        for (int ch = 1;
                            ch <= Math.Min(an.Length, 12);
                            ch++)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = $"{PdfFormFields.An}{ch}",
                                Value = $"{an[ch - 1]}"
                            });
                        }
                    }
                    if (contract.PrimaryCustomer.Locations.FirstOrDefault(m => m.AddressType == AddressType.MainAddress).State == "QC")
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.DateOfAgreement,
                            Value = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.FirstPaymentDate,
                            Value = DateTime.Now.AddMonths(1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.MonthlyPaymentDate,
                            Value = DateTime.Now.Day.ToString()
                        });
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.FinalPaymentDate,
                            Value = DateTime.Now.AddMonths(contract.Equipment.LoanTerm + 1 ?? 1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        });
                    }
                }
            }
        }

        private void FillExistingEquipmentFields(List<FormField> formFields, Contract contract)
        {
            if (contract?.Equipment?.ExistingEquipment?.Any() == true)
            {
                var fstEq = contract.Equipment.ExistingEquipment.First();
                if (fstEq.ResponsibleForRemoval == ResponsibleForRemovalType.Customer)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.ExistingEquipmentRemovalCustomer,
                        Value = "true"
                    });
                }
                if (fstEq.ResponsibleForRemoval == ResponsibleForRemovalType.ExistingSupplier)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.ExistingEquipmentRemovalSupplier,
                        Value = "true"
                    });
                }
                if (fstEq.ResponsibleForRemoval == ResponsibleForRemovalType.NA)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.ExistingEquipmentRemovalNA,
                        Value = "true"
                    });
                }
                if (fstEq.ResponsibleForRemoval == ResponsibleForRemovalType.Other)
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = PdfFormFields.ExistingEquipmentRemovalOther,
                        Value = "true"
                    });
                    if (!string.IsNullOrEmpty(fstEq.ResponsibleForRemovalValue))
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.ExistingEquipmentRemovalOtherValue,
                            Value = fstEq.ResponsibleForRemovalValue
                        });
                    }
                }

                var serials = contract.Equipment.ExistingEquipment.Select(ee => ee.SerialNumber).JoinStrings("\r\n");                
                if (serials.Any())
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.ExistingEquipmentSerialNumber,
                        Value = serials
                    });
                }

                //if (_contractRepository.IsBill59Contract(contract.Id))
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.CheckBox,
                        Name = fstEq.IsRental
                            ? PdfFormFields.IsExistingEquipmentRental
                            : PdfFormFields.IsExistingEquipmentNoRental,
                        Value = "true"
                    });
                    if (fstEq.IsRental)
                    {
                        formFields.Add(new FormField()
                        {
                            FieldType = FieldType.Text,
                            Name = PdfFormFields.ExistingEquipmentRentalCompany,
                            Value = fstEq.RentalCompany
                        });
                    }
                }
            }
        }        
        private void FillDealerFields(List<FormField> formFields, Contract contract)
        {
            if (!string.IsNullOrEmpty(contract?.Equipment?.SalesRep))
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.SalesRep,
                    Value = contract.Equipment.SalesRep
                });
            }

            if (contract?.SalesRepInfo?.ConcludedAgreement == true)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.CheckBox,
                    Name = PdfFormFields.SalesRepConcludedAgreement,
                    Value = "true"
                });
            }
            if (contract?.SalesRepInfo?.NegotiatedAgreement == true)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.CheckBox,
                    Name = PdfFormFields.SalesRepNegotiatedAgreement,
                    Value = "true"
                });
            }
            if (contract?.SalesRepInfo?.InitiatedContact == true)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.CheckBox,
                    Name = PdfFormFields.SalesRepInitiatedContact,
                    Value = "true"
                });
            }

            //try to get Dealer info from Aspire and fill it
            if (!string.IsNullOrEmpty(contract?.Dealer.AspireLogin))
            {
                TimeSpan aspireRequestTimeout = TimeSpan.FromSeconds(5);
                Task timeoutTask = Task.Delay(aspireRequestTimeout);
                var aspireRequestTask =
                    Task.Run(() => _aspireStorageReader.GetDealerInfo(contract?.Dealer.AspireLogin));

                try
                {
                    if (Task.WhenAny(aspireRequestTask, timeoutTask).ConfigureAwait(false).GetAwaiter().GetResult() ==
                        aspireRequestTask)
                    {
                        var dealerInfo = AutoMapper.Mapper.Map<DealerDTO>(aspireRequestTask.Result);

                        if (dealerInfo != null)
                        {
                            formFields.Add(new FormField()
                            {
                                FieldType = FieldType.Text,
                                Name = PdfFormFields.DealerName,
                                Value = dealerInfo.FirstName
                            });
                            var dealerAddress =
                                dealerInfo.Locations?.FirstOrDefault();
                            if (dealerAddress != null)
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.DealerAddress,
                                    Value =
                                        $"{dealerAddress.Street}, {dealerAddress.City}, {dealerAddress.State}, {dealerAddress.PostalCode}"
                                });
                            }

                            if (dealerInfo.Phones?.Any() ?? false)
                            {
                                formFields.Add(new FormField()
                                {
                                    FieldType = FieldType.Text,
                                    Name = PdfFormFields.DealerPhone,
                                    Value = dealerInfo.Phones.First().PhoneNum
                                });
                            }
                       }
                    }
                    else
                    {
                        _loggingService.LogError("Cannot get dealer info from Aspire");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Cannot get dealer info from Aspire database", ex);
                }
            }
        }

        private void FillExistingAgreementsInfoFields(List<FormField> formFields, Contract contract)
        {
            var agreements = _aspireStorageReader.SearchCustomerAgreements(contract.PrimaryCustomer.FirstName,
                contract.PrimaryCustomer.LastName, contract.PrimaryCustomer.DateOfBirth);
            if (agreements?.Any() == true)
            {
                var loans = agreements.Where(a =>
                    string.Compare(a.Type, "loan", StringComparison.InvariantCultureIgnoreCase) == 0);
                var rentals = agreements.Where(a =>
                    string.Compare(a.Type, "rental", StringComparison.InvariantCultureIgnoreCase) == 0);
                if (loans.Any())
                {
                    var gLoans = loans.GroupBy(l => l.TransactionId).Take(3);
                    var loansDescr = gLoans.Select(g =>
                            Resources.Resources.LoanApplication + ": " +
                            g.Select(s => s.EquipTypeDesc).JoinStrings(", "))
                        .JoinStrings("; ");
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.ExistingLoansDescription,
                        Value = loansDescr
                    });
                }
                if (rentals.Any())
                {
                    var gRentals = rentals.GroupBy(l => l.TransactionId).Take(3);
                    var rentalsDescr = gRentals.Select(g =>
                            Resources.Resources.RentalApplication + ": " +
                            g.Select(s => s.EquipTypeDesc).JoinStrings(", "))
                        .JoinStrings("\r\n");
                    var rentalsStartDate = gRentals.Select(g => g.FirstOrDefault()?.StartDate.ToShortDateString()).JoinStrings("\r\n");
                    var rentalsTerminationDate = gRentals.Select(g => g.FirstOrDefault()?.MaturityDate.ToShortDateString()).JoinStrings("\r\n");
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.ExistingRentalsDescription,
                        Value = rentalsDescr
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.ExistingRentalsDateEntered,
                        Value = rentalsStartDate
                    });
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.ExistingRentalsTerminationDate,
                        Value = rentalsTerminationDate
                    });
                }
            }

            //var serials = contract.Equipment.ExistingEquipment.Select(ee => ee.SerialNumber).JoinStrings("\r\n");
        }

        private void FillInstallCertificateFields(List<FormField> formFields, Contract contract)
        {
            formFields.Add(new FormField()
            {
                FieldType = FieldType.Text,
                Name = PdfFormFields.InstallerName,
                Value = $"{contract.Equipment.InstallerLastName} {contract.Equipment.InstallerFirstName}"
            });
            if (contract.Equipment.InstallationDate.HasValue)
            {
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.InstallationDate,
                    Value =
                        contract.Equipment?.InstallationDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)
                });
            }

            if (contract.Equipment.NewEquipment?.Any() ?? false)
            {
                string eqDescs = string.Empty;
                string eqModels = string.Empty;
                string eqSerials = string.Empty;
                contract.Equipment.NewEquipment.ForEach(eq =>
                {
                    if (!string.IsNullOrEmpty(eqModels))
                    {
                        eqModels += "; ";
                    }
                    eqModels += eq.InstalledModel;
                    if (!string.IsNullOrEmpty(eqSerials))
                    {
                        eqSerials += "; ";
                    }
                    eqSerials += eq.InstalledSerialNumber;
                    if (!string.IsNullOrEmpty(eqDescs))
                    {
                        eqDescs += "; ";
                    }
                    eqDescs += eq.Description;
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EquipmentModel,
                    Value = eqModels
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.EquipmentSerialNumber,
                    Value = eqSerials
                });
                var descField = formFields.FirstOrDefault(f => f.Name == PdfFormFields.EquipmentDescription);
                if (descField != null)
                {
                    descField.Value = eqDescs;
                }
                else
                {
                    formFields.Add(new FormField()
                    {
                        FieldType = FieldType.Text,
                        Name = PdfFormFields.EquipmentDescription,
                        Value = eqDescs
                    });
                }
            }

            if (contract.Equipment.ExistingEquipment?.Any() ?? false)
            {
                var exEq = contract.Equipment.ExistingEquipment.First();
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.CheckBox,
                    Name = exEq.IsRental
                        ? PdfFormFields.IsExistingEquipmentRental
                        : PdfFormFields.IsExistingEquipmentNoRental,
                    Value = "true"
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentRentalCompany,
                    Value = exEq.RentalCompany
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentMake,
                    Value = exEq.Make
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentModel,
                    Value = exEq.Model
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentSerialNumber,
                    Value = exEq.SerialNumber
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentGeneralCondition,
                    Value = exEq.GeneralCondition
                });
                formFields.Add(new FormField()
                {
                    FieldType = FieldType.Text,
                    Name = PdfFormFields.ExistingEquipmentAge,
                    Value = exEq.EstimatedAge.ToString("F", CultureInfo.InvariantCulture)
                });
            }
        }

        private void ReformatTempalteNameWithId(AgreementDocument document, string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return;
            }

            var defaultName = document.Name;
            document.Name = $"{transactionId} {defaultName}";
        }

        #endregion
    }
}