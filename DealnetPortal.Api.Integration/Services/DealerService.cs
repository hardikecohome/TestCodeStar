using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.DealerOnboarding;
using DealnetPortal.Api.Models.Profile;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using DealnetPortal.Api.Models.Notify;
using DealnetPortal.Domain.Repositories;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services
{
    public class DealerService : IDealerService
    {
        private readonly IDealerRepository _dealerRepository;
        private readonly IDealerOnboardingRepository _dealerOnboardingRepository;
        private readonly IAspireService _aspireService;
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly IContractRepository _contractRepository;
        private readonly ILoggingService _loggingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMailService _mailService;
        private readonly IAppConfiguration _configuration;

        public DealerService(IDealerRepository dealerRepository, IDealerOnboardingRepository dealerOnboardingRepository, 
            IAspireService aspireService, IAspireStorageReader aspireStorageReader, ILoggingService loggingService, IUnitOfWork unitOfWork, IContractRepository contractRepository, IMailService mailService,
            IAppConfiguration configuration)
        {
            _dealerRepository = dealerRepository;
            _dealerOnboardingRepository = dealerOnboardingRepository;
            _aspireService = aspireService;
            _aspireStorageReader = aspireStorageReader;
            _loggingService = loggingService;
            _unitOfWork = unitOfWork;
            _contractRepository = contractRepository;
            _mailService = mailService;
            _configuration = configuration;
        }

        public DealerProfileDTO GetDealerProfile(string dealerId)
        {
            var profile = _dealerRepository.GetDealerProfile(dealerId);
            return Mapper.Map<DealerProfileDTO>(profile);
        }

        public string GetDealerParentName(string dealerId)
        {
            var parentName = _contractRepository.GetDealer(_dealerRepository.GetParentDealerId(dealerId)?? dealerId).AspireLogin;
            return parentName;
        }

        public IList<Alert> DealerSupportRequestEmail(SupportRequestDTO dealerSupportRequest)
        {
            var alerts = new List<Alert>();
            try
            {
                var dealerProvince = _aspireStorageReader.GetDealerInfo(dealerSupportRequest.DealerName).State;

                var result = _mailService.SendSupportRequiredEmail(dealerSupportRequest, dealerProvince);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to send Dealer support request for [{dealerSupportRequest.YourName}] dealer with support ID [{dealerSupportRequest.Id}]", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.FailedToUpdateSettings,
                    Message = "Failed to send Dealer support request"
                });
            }

            return alerts;
        }

        public IList<Alert> UpdateDealerProfile(DealerProfileDTO dealerProfile)
        {
            var alerts = new List<Alert>();

            try
            {
                var profile = Mapper.Map<DealerProfile>(dealerProfile);
                var newProfile = _dealerRepository.UpdateDealerProfile(profile);
                if (newProfile != null)
                {

                    _unitOfWork.Save();
                    var dealer = _contractRepository.GetDealer(newProfile.DealerId);
                    dealer.DealerProfileId = newProfile.Id;
                    _dealerRepository.UpdateDealer(dealer);
                    _unitOfWork.Save();
                }
                else
                {
                    _loggingService.LogError($"Failed to update a dealer profile for [{dealerProfile.DealerId}] dealer");
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Code = ErrorCodes.FailedToUpdateSettings,
                        Message = "Failed to update a dealer profile"
                    });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to update a dealer profile for [{dealerProfile.DealerId}] dealer", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.FailedToUpdateSettings,
                    Message = "Failed to update a dealer profile"
                });
            }
            return alerts;
        }

        public DealerInfoDTO GetDealerOnboardingForm(string accessKey)
        {
            var dealerInfo = _dealerOnboardingRepository.GetDealerInfoByAccessKey(accessKey);
            RevertUnprocessedDocuments(dealerInfo);
            var mappedInfo = Mapper.Map<DealerInfoDTO>(dealerInfo);
            mappedInfo.SalesRepLink = _contractRepository.GetDealer(dealerInfo.ParentSalesRepId)?.OnboardingLink;

            return mappedInfo;
        }

        public DealerInfoDTO GetDealerOnboardingForm(int id)
        {
            var dealerInfo = _dealerOnboardingRepository.GetDealerInfoById(id);
            RevertUnprocessedDocuments(dealerInfo);
            var mappedInfo = Mapper.Map<DealerInfoDTO>(dealerInfo);
            return mappedInfo;
        }

        public async Task<Tuple<DealerInfoKeyDTO, IList<Alert>>> UpdateDealerOnboardingForm(DealerInfoDTO dealerInfo)
        {
            if (dealerInfo == null)
            {
                throw new ArgumentNullException(nameof(dealerInfo));
            }

            var alerts = new List<Alert>();
            DealerInfoKeyDTO resultKey = null;
            try
            {
                var mappedInfo = Mapper.Map<DealerInfo>(dealerInfo);
                mappedInfo.ParentSalesRepId = mappedInfo.ParentSalesRepId ??
                                              _dealerRepository.GetUserIdByOnboardingLink(dealerInfo.SalesRepLink);
                var updatedInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(mappedInfo);
                _unitOfWork.Save();

                ProcessDocuments(updatedInfo);

                resultKey = new DealerInfoKeyDTO()
                {
                    AccessKey = updatedInfo.AccessKey,
                    DealerInfoId = updatedInfo.Id
                };
                
                //submit draft form to Aspire                                             
                var reSubmit = updatedInfo.SentToAspire;                
                string statusToSet = !string.IsNullOrEmpty(updatedInfo.TransactionId) ? _aspireStorageReader.GetDealStatus(updatedInfo.TransactionId) ?? updatedInfo.Status : null;
                var submitResult = await _aspireService.SubmitDealerOnboarding(updatedInfo.Id, dealerInfo.LeadSource);
                if (submitResult?.Any() ?? false)
                {
                    //for draft aspire errors is not important and can be by not full set of data
                    submitResult.Where(r => r.Type == AlertType.Error).ForEach(r => r.Type = AlertType.Warning);
                    alerts.AddRange(submitResult);
                }
                if (reSubmit || !string.IsNullOrEmpty(statusToSet))
                {
                    //if we don't send any docs, just change status in Aspire to needed
                    var result = await
                        _aspireService.ChangeDealStatusEx(updatedInfo.TransactionId,
                            statusToSet ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_DRAFT_STATUS_KEY),
                            updatedInfo.ParentSalesRepId);
                    //TODO save status
                    if (!string.IsNullOrEmpty(result.Item1))
                    {
                        updatedInfo.Status = result.Item1;
                        _unitOfWork.Save(); ;
                    }
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Header = "Cannot update dealer onboarding info",
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });                
            }
            return new Tuple<DealerInfoKeyDTO, IList<Alert>>(resultKey, alerts);
        }

        public async Task<Tuple<DealerInfoKeyDTO, IList<Alert>>> SubmitDealerOnboardingForm(DealerInfoDTO dealerInfo)
        {
            if (dealerInfo == null)
            {
                throw new ArgumentNullException(nameof(dealerInfo));
            }

            var alerts = new List<Alert>();
            DealerInfoKeyDTO resultKey = null;
            try
            {
                //update draft in a database as we should have it with required documents 
                var mappedInfo = Mapper.Map<DealerInfo>(dealerInfo);
                mappedInfo.ParentSalesRepId = mappedInfo.ParentSalesRepId ??
                                              _dealerRepository.GetUserIdByOnboardingLink(dealerInfo.SalesRepLink);
                var updatedInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(mappedInfo);
                _unitOfWork.Save();

                ProcessDocuments(updatedInfo);

                resultKey = new DealerInfoKeyDTO()
                {
                    AccessKey = updatedInfo.AccessKey,
                    DealerInfoId = updatedInfo.Id
                };

                //submit form to Aspire                                             
                var reSubmit = updatedInfo.SentToAspire;
                string statusToSet = reSubmit ? _aspireStorageReader.GetDealStatus(updatedInfo.TransactionId) ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY) : _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY);
                var submitResult = await _aspireService.SubmitDealerOnboarding(updatedInfo.Id, dealerInfo.LeadSource);
                if (submitResult?.Any() ?? false)
                {
                    alerts.AddRange(submitResult);
                }
                if (submitResult?.Any(r => r.Type == AlertType.Error) == true)
                {
                    //notify dealnet here about failed upload to Aspire
                    var errorMsg = string.Concat(submitResult.Where(x => x.Type == AlertType.Error).Select(r => r.Header + ": " + r.Message).ToArray());
                    await _mailService.SendProblemsWithSubmittingOnboarding(errorMsg, updatedInfo.Id, mappedInfo.AccessKey);
                }
                else
                {
                    if (!reSubmit)
                    {
                        updatedInfo.Status = statusToSet;
                        updatedInfo.SentToAspire = true;
                        _unitOfWork.Save();
                    }
                }
                //upload required documents
                if (updatedInfo.RequiredDocuments?.Any(d => !d.Uploaded) == true)
                {
                    UploadOnboardingDocuments(updatedInfo.Id, statusToSet ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY));                                        
                }
                else
                {
                    if (!reSubmit || !string.IsNullOrEmpty(statusToSet))
                    {
                        //if we don't send any docs, just change status in Aspire to needed
                        var result = await
                            _aspireService.ChangeDealStatusEx(updatedInfo.TransactionId,
                                statusToSet ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY),
                                updatedInfo.ParentSalesRepId);
                        if (!string.IsNullOrEmpty(result.Item1) && updatedInfo.Status != result.Item1)
                        {
                            updatedInfo.Status = result.Item1;
                            _unitOfWork.Save();
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                var errorMsg = $"Cannot submit dealer onboarding form";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.FailedToUpdateContract,
                    Header = errorMsg,
                    Message = ex.ToString()
                });
                _loggingService.LogError(errorMsg, ex);
            }
            return new Tuple<DealerInfoKeyDTO, IList<Alert>>(resultKey, alerts);
        }

        public async Task<IList<Alert>> SendDealerOnboardingDraftLink(DraftLinkDTO link)
        {
            if (string.IsNullOrEmpty(link.AccessKey))
            {
                throw new ArgumentNullException(nameof(link));
            }

            var alerts = new List<Alert>();
            try
            {
                await _mailService.SendDraftLinkMail(link.AccessKey, link.Email);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Cannot send draf link by email";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.SubmitFailed,
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg);
            }
            return alerts;
        }

        public bool CheckOnboardingLink(string dealerLink)
        {
            return _dealerRepository.GetUserIdByOnboardingLink(dealerLink) != null;
        }

        public async Task<Tuple<DealerInfoKeyDTO, IList<Alert>>> AddDocumentToOnboardingForm(RequiredDocumentDTO document)
        {
            var alerts = new List<Alert>();
            DealerInfoKeyDTO resultKey = null;
            try
            {
                var mappedDoc = Mapper.Map<RequiredDocument>(document);
                var updatedDoc = _dealerOnboardingRepository.AddDocumentToDealer(mappedDoc.DealerInfoId, mappedDoc);
                _unitOfWork.Save();                                
                resultKey = new DealerInfoKeyDTO()
                {
                    AccessKey = updatedDoc.DealerInfo?.AccessKey,
                    DealerInfoId = updatedDoc.DealerInfo?.Id ?? 0,
                    ItemId = updatedDoc.Id
                };
                //if form was submitted before, we can upload document to Aspire
                //if (updatedDoc.DealerInfo?.SentToAspire == true &&
                //    !string.IsNullOrEmpty(updatedDoc.DealerInfo?.TransactionId))
                //{
                //    var status = await _aspireService.GetDealStatus(updatedDoc.DealerInfo.TransactionId);
                //    var uAlerts = await _aspireService.UploadOnboardingDocument(updatedDoc.DealerInfo.Id, updatedDoc.Id, !string.IsNullOrEmpty(status) ? status : null);
                //    if (uAlerts?.Any() == true)
                //    {
                //        alerts.AddRange(uAlerts);
                //    }
                //    if (!string.IsNullOrEmpty(status) && updatedDoc.DealerInfo.Status != status)
                //    {
                //        updatedDoc.DealerInfo.Status = status;
                //        _unitOfWork.Save();
                //    }
                //}
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Header = "Cannot add document to a dealer onboarding info",
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });
            }
            return new Tuple<DealerInfoKeyDTO, IList<Alert>>(resultKey, alerts);
        }

        public IList<Alert> DeleteDocumentFromOnboardingForm(RequiredDocumentDTO document)
        {
            var alerts = new List<Alert>();
            try
            {
                if (document.DealerInfoId.HasValue)
                {
                    var dealerInfo = _dealerOnboardingRepository.GetDealerInfoById(document.DealerInfoId.Value);
                    if (dealerInfo != null)
                    {
                        _dealerOnboardingRepository.SetDocumentStatus(document.Id, DocumentStatus.Removing);//DeleteDocumentFromDealer(document.Id);

                        _unitOfWork.Save();
                    }
                    else
                    {
                        alerts.Add(new Alert()
                        {
                            Header = "Cannot delete document from a dealer onboarding info",
                            Type = AlertType.Error,
                            Message = "Info of this dealer not exists."
                        });
                    }
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Header = "Cannot delete document from a dealer onboarding info",
                        Type = AlertType.Error,
                        Message = "No dealer info id."
                    });
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Header = "Cannot delete document from a dealer onboarding info",
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public IList<Alert> DeleteDealerOnboardingForm(int dealerInfoId)
        {
            var alerts = new List<Alert>();
            try
            {
                if (_dealerOnboardingRepository.DeleteDealerInfo(dealerInfoId))
                {
                    _unitOfWork.Save();                    
                }                
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Header = "Cannot delete dealer onboarding info",
                    Type = AlertType.Error,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        private void UploadOnboardingDocuments(int dealerInfoId, string statusToSend = null)
        {
            var dealerInfo = _dealerOnboardingRepository.GetDealerInfoById(dealerInfoId);
            if (dealerInfo?.RequiredDocuments?.Any(d => d.DocumentBytes != null && !d.Uploaded) == true)
            {
                Task.Run(async () =>
                {
                    dealerInfo.RequiredDocuments.Where(d => !d.Uploaded).ForEach(doc =>
                    {
                        _aspireService.UploadOnboardingDocument(dealerInfoId, doc.Id, statusToSend).GetAwaiter().GetResult();                        
                    });
                    try
                    {
                        var tryChangeByCreditReview =
                            await _aspireService.ChangeDealStatusByCreditReview(dealerInfo.TransactionId, statusToSend ?? _configuration.GetSetting(WebConfigKeys.ONBOARDING_INIT_STATUS_KEY), dealerInfo.ParentSalesRepId);
                    }
                    catch (Exception e)
                    {
                        //alerts.Add(new Alert()
                        //{
                        //    Type = AlertType.Warning,
                        //    Code = ErrorCodes.FailedToUpdateContract,
                        //    Header = "Cannot update onboarding status in Aspire",
                        //    Message = e.ToString()
                        //});
                        _loggingService.LogWarning($"Cannot update onboarding form [{dealerInfoId}] status in Aspire:{e.ToString()}");
                    }
                });
            }            
        }

        private void ProcessDocuments(DealerInfo dealerInfo)
        {
            var docForProcess = dealerInfo.RequiredDocuments?.Where(d => d.Status != null).ToList();
            if (docForProcess?.Any() == true)
            {
                docForProcess.ForEach(d =>
                {
                    switch (d.Status)
                    {                        
                        case DocumentStatus.Removing:
                            _dealerOnboardingRepository.DeleteDocumentFromDealer(d.Id);
                            break;
                        case DocumentStatus.Adding:
                            d.Status = null;                        
                            break;                                                
                    }
                });
                _unitOfWork.Save();
            }
        }

        private void RevertUnprocessedDocuments(DealerInfo dealerInfo)
        {
            var docForProcess = dealerInfo.RequiredDocuments?.Where(d => d.Status != null).ToList();
            if (docForProcess?.Any() == true)
            {
                docForProcess.ForEach(d =>
                {
                    switch (d.Status)
                    {
                        case DocumentStatus.Removing:
                            d.Status = null;                            
                            break;
                        case DocumentStatus.Adding:
                            _dealerOnboardingRepository.DeleteDocumentFromDealer(d.Id);
                            break;                        
                    }
                });
                _unitOfWork.Save();
            }
        }
    }
}
