using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Contract.EquipmentInformation;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services
{
    public class CustomerFormService : ICustomerFormService
    {
        private readonly IContractRepository _contractRepository;
        private readonly ICustomerFormRepository _customerFormRepository;
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly IMailService _mailService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDealerRepository _dealerRepository;
        private readonly IContractService _contractService;
        private readonly ICreditCheckService _creditCheckService;
        private readonly ILoggingService _loggingService;
        private readonly IAppConfiguration _configuration;

        public CustomerFormService(IContractRepository contractRepository,
            ICustomerFormRepository customerFormRepository,
            IDealerRepository dealerRepository, ISettingsRepository settingsRepository, IUnitOfWork unitOfWork,
            IContractService contractService,
            ILoggingService loggingService, IMailService mailService,
            IAspireStorageReader aspireStorageReader, IAppConfiguration configuration, ICreditCheckService creditCheckService)
        {
            _contractRepository = contractRepository;
            _customerFormRepository = customerFormRepository;
            _dealerRepository = dealerRepository;
            _aspireStorageReader = aspireStorageReader;
            _settingsRepository = settingsRepository;
            _mailService = mailService;
            _unitOfWork = unitOfWork;
            _contractService = contractService;
            _loggingService = loggingService;
            _configuration = configuration;
            _creditCheckService = creditCheckService;
        }

        public CustomerLinkDTO GetCustomerLinkSettings(string dealerId)
        {
            var linkSettings = _customerFormRepository.GetCustomerLinkSettings(dealerId);
            if (linkSettings != null)
            {
                return Mapper.Map<CustomerLinkDTO>(linkSettings);
            }
            return null;
        }

        public CustomerLinkDTO GetCustomerLinkSettingsByDealerName(string dealerName)
        {
            var linkSettings = _customerFormRepository.GetCustomerLinkSettingsByDealerName(dealerName);
            if (linkSettings != null)
            {
                return Mapper.Map<CustomerLinkDTO>(linkSettings);
            }
            return null;
        }

        public CustomerLinkLanguageOptionsDTO GetCustomerLinkLanguageOptions(string hashDealerName, string language)
        {
            var linkSettings = _customerFormRepository.GetCustomerLinkSettingsByHashDealerName(hashDealerName);
            if (linkSettings != null)
            {
                var langSettings = new CustomerLinkLanguageOptionsDTO
                {
                    IsLanguageEnabled =
                        linkSettings.EnabledLanguages.FirstOrDefault(l => l.Language?.Code == language) != null
                };
                if (langSettings.IsLanguageEnabled)
                {
                    langSettings.LanguageServices =
                        linkSettings.Services.Where(
                            s =>
                                s.LanguageId ==
                                linkSettings.EnabledLanguages.First(l => l.Language?.Code == language).LanguageId)
                            .Select(s => s.Service).ToList();
                }
                langSettings.EnabledLanguages =
                    linkSettings.EnabledLanguages.Select(l => (LanguageCode) l.LanguageId).ToList();
                langSettings.DealerName = _dealerRepository.GetDealerNameByCustomerLinkId(linkSettings.Id);
                var dealerId = _dealerRepository.GetUserIdByName(langSettings.DealerName);
                if (!string.IsNullOrEmpty(dealerId))
                {
                    var dealer = _dealerRepository.GetDealerProfile(dealerId);
                    var province=dealer.Address.State?.ToProvinceCode();
                    langSettings.QuebecDealer = province != null && province == "QC";
                }
                return langSettings;
            }
            return null;
        }

        public IList<Alert> UpdateCustomerLinkSettings(CustomerLinkDTO customerLinkSettings, string dealerId)
        {
            var alerts = new List<Alert>();
            try
            {
                var linkSettings = Mapper.Map<CustomerLink>(customerLinkSettings);
                CustomerLink updatedLink = null;
                if (linkSettings.EnabledLanguages != null)
                {
                    updatedLink = _customerFormRepository.UpdateCustomerLinkLanguages(linkSettings.EnabledLanguages, customerLinkSettings.HashLink, dealerId);
                }
                if (linkSettings.Services != null)
                {
                    updatedLink = _customerFormRepository.UpdateCustomerLinkServices(linkSettings.Services, customerLinkSettings.HashLink, dealerId) ?? updatedLink;
                }
                if (updatedLink != null)
                {
                    updatedLink.HashLink = customerLinkSettings.HashLink;
                    _unitOfWork.Save();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to update a customer link settings for [{dealerId}] dealer", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.FailedToUpdateSettings,
                    Header = "Failed to update a customer link settings",
                    Message = "Failed to update a customer link settings"
                });
            }
            return alerts;
        }

        public Tuple<CustomerContractInfoDTO, IList<Alert>> SubmitCustomerFormData(CustomerFormDTO customerFormData)
        {
            if (customerFormData == null)
            {
                throw new ArgumentNullException(nameof(customerFormData));
            }

            var contractCreationRes = CreateContractByCustomerFormData(customerFormData);
            if (contractCreationRes?.Item1 != null &&
                (contractCreationRes.Item2?.All(a => a.Type != AlertType.Error) ?? true))
            {
                Task.Run(async () => await SendCustomerFormContractCreationNotifications(customerFormData,
                    contractCreationRes.Item1));
            }

            return new Tuple<CustomerContractInfoDTO, IList<Alert>>(contractCreationRes?.Item1,
                contractCreationRes?.Item2 ?? new List<Alert>());
        }

        public async Task<Tuple<IList<CustomerContractInfoDTO>, IList<Alert>>> CustomerServiceRequest(
            CustomerServiceRequestDTO customerFormData)
        {
            var alerts = new List<Alert>();
            IList<CustomerContractInfoDTO> submitResults = null;

            if (customerFormData?.ServiceRequests?.Any() != true)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.ContractCreateFailed,
                    Header = "Request doesn't contain improvements",
                    Message = "Request doesn't contain improvements"
                });
            }

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                IList<Contract> newContracts = null;
                try
                {
                    newContracts =
                        customerFormData?.ServiceRequests?.Select(
                            sRequest =>
                            {
                                var dealerId = _dealerRepository.GetUserIdByName(sRequest.DealerName);
                                if (!string.IsNullOrEmpty(dealerId))
                                {
                                    var c = InitialyzeContract(dealerId, customerFormData.PrimaryCustomer,
                                        customerFormData.StartProjectDate,
                                        sRequest.PrecreatedContractId, sRequest.ServiceType,
                                        customerFormData.CustomerComment, customerFormData.LeadSource, customerFormData.ContractorInfo);
                                    // mark as created by customer
                                    c.IsCreatedByCustomer = true;
                                    //check role of a dealer
                                    c.IsCreatedByBroker = _dealerRepository.GetUserRoles(dealerId)
                                        .Contains(UserRole.MortgageBroker.ToString());

                                    return c;
                                }
                                return null;
                            }).ToList();
                    _unitOfWork.Save();
                }
                catch (Exception ex)
                {
                    var errorMsg =
                        "Cannot create contract from customer wallet request";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Warning, //?
                        Code = ErrorCodes.ContractCreateFailed,
                        Header = ErrorConstants.ContractCreateFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg, ex);
                }

                newContracts = newContracts?.Where(i => i != null).ToList();

                //Credit check
                newContracts?.ForEach(c =>
                {
                    //Do credit check only for new contract (not updated from CW)
                    if (c.ContractState < ContractState.CreditConfirmed)
                    {
                        //Start credit check for this contract                            
                        var creditCheckAlerts = new List<Alert>();                        
                        var checkResult = _creditCheckService.ContractCreditCheck(c.Id, c.DealerId);
                        if (checkResult != null)
                        {
                            creditCheckAlerts.AddRange(checkResult.Item2);
                        }
                        if (creditCheckAlerts.Any())
                        {
                            alerts.AddRange(creditCheckAlerts);
                        }
                    }
                });

                if (newContracts?.Any() == true)
                {
                    newContracts.ForEach(c =>
                    {
                        c.IsNewlyCreated = true;
                        c.IsCreatedByCustomer = true;
                        if (customerFormData.ContractorInfo != null)
                        {
                            c.Dealer = null;
                            c.DealerId = null;
                        }
                    });
                    _unitOfWork.Save();

                    submitResults = newContracts.Select(c => GetCustomerContractInfo(c.Id, c.Dealer?.UserName)).ToList();
                    if (newContracts.Select(x => x.Equipment.NewEquipment.FirstOrDefault()).Any(i => i != null) &&
                        newContracts.FirstOrDefault()?.PrimaryCustomer.Locations
                            .FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress) != null)
                    {
                        await _mailService.SendHomeImprovementMailToCustomer(newContracts).ConfigureAwait(false);
                        newContracts.ForEach(c =>
                        {
                            if (_contractRepository.IsContractUnassignable(c.Id))
                            {
                                var nowait = _mailService.SendNotifyMailNoDealerAcceptLead(c);
                            }
                        });
                    }


                }
            }
            if (alerts.Any(a => a.Type == AlertType.Error))
            {
                _loggingService.LogError("Cannot create contract by customer loan form request");
            }
            return new Tuple<IList<CustomerContractInfoDTO>, IList<Alert>>(submitResults, alerts);
        }

        public CustomerContractInfoDTO GetCustomerContractInfo(int contractId, string dealerName)
        {
            CustomerContractInfoDTO contractInfo = null;
            var creditReviewStates = _configuration.GetSetting(WebConfigKeys.CREDIT_REVIEW_STATUS_CONFIG_KEY).Split(',').Select(s => s.Trim()).ToArray();
            if (string.IsNullOrEmpty(dealerName))
            {
                var contract = _contractRepository.GetContract(contractId);
                if (contract != null)
                {
                    contractInfo = new CustomerContractInfoDTO()
                    {
                        ContractId = contractId,
                        AccountId = contract.PrimaryCustomer?.AccountId,
                        DealerName = contract.LastUpdateOperator,
                        TransactionId = contract.Details?.TransactionId,
                        ContractState = contract.ContractState,
                        Status = contract.Details?.Status,
                        CreditAmount = contract.Details?.CreditAmount ?? 0,
                        ScorecardPoints = contract.Details?.ScorecardPoints ?? 0,
                        CreationTime = contract.CreationTime,
                        LastUpdateTime = contract.LastUpdateTime,
                        EquipmentTypes = contract.Equipment?.NewEquipment?.Select(e => e.Type).ToList(),
                        IsPreApproved = contract.ContractState == ContractState.CreditConfirmed && !creditReviewStates.Contains(contract.Details?.Status)
                    };
                }
            }
            else
            {
                var dealerId = _dealerRepository.GetUserIdByName(dealerName);
                if (!string.IsNullOrEmpty(dealerId))
                {
                    var contract = _contractRepository.GetContract(contractId, dealerId);
                    if (contract != null)
                    {
                        contractInfo = new CustomerContractInfoDTO()
                        {
                            ContractId = contractId,
                            AccountId = contract.PrimaryCustomer?.AccountId,
                            DealerName = contract.LastUpdateOperator,
                            TransactionId = contract.Details?.TransactionId,
                            ContractState = contract.ContractState,
                            Status = contract.Details?.Status,
                            CreditAmount = contract.Details?.CreditAmount ?? 0,
                            ScorecardPoints = contract.Details?.ScorecardPoints ?? 0,
                            CreationTime = contract.CreationTime,
                            LastUpdateTime = contract.LastUpdateTime,
                            EquipmentTypes = contract.Equipment?.NewEquipment?.Select(e => e.Type).ToList(),
                            IsPreApproved = contract.ContractState == ContractState.CreditConfirmed && !creditReviewStates.Contains(contract.Details?.Status),
                            DealerType =  contract.Dealer?.DealerType
                        };

                        if (contract.Dealer?.Tier?.IsCustomerRisk == true &&
                            contract.ContractState > ContractState.CustomerInfoInputted)
                        {
                            contractInfo.CustomerBeacon = contract.PrimaryCustomer?.CreditReport?.Beacon ?? 0;
                        }

                        try
                        {
                            //get dealer info
                            var dealer = Mapper.Map<DealerDTO>(_aspireStorageReader.GetDealerInfo(dealerName));
                            if (dealer != null)
                            {
                                contractInfo.DealerName = dealer.FirstName;
                                var dealerAddress = dealer.Locations?.FirstOrDefault();
                                if (dealerAddress != null)
                                {
                                    contractInfo.DealerAdress = dealerAddress;
                                }
                                if (dealer.Phones?.Any() ?? false)
                                {
                                    contractInfo.DealerPhone = dealer.Phones.First().PhoneNum;
                                }
                                if (dealer.Emails?.Any() ?? false)
                                {
                                    contractInfo.DealerEmail = dealer.Emails.First().EmailAddress;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = "Can't retrieve dealer info";
                            _loggingService.LogError(errorMsg, ex);
                        }

                    }
                }
            }
            return contractInfo;
        }

        private Tuple<CustomerContractInfoDTO, IList<Alert>> CreateContractByCustomerFormData(
            CustomerFormDTO customerFormData)
        {
            var alerts = new List<Alert>();
            CustomerContractInfoDTO submitResult = null;

            var dealerId = _dealerRepository.GetUserIdByName(customerFormData.DealerName);
            if (!string.IsNullOrEmpty(dealerId))
            {
                Contract contract = _contractRepository.CreateContract(dealerId);
                _unitOfWork.Save();
                _loggingService.LogInfo(
                    $"Created new contract [{contract.Id}] by customer loan form request for {customerFormData.DealerName} dealer");

                var contractData = new ContractDataDTO()
                {
                    PrimaryCustomer = customerFormData.PrimaryCustomer,
                    DealerId = dealerId,
                    Id = contract.Id,
                    LeadSource = customerFormData.LeadSource
                };
                _contractService.UpdateContractData(contractData, dealerId);

                //Start credit check for this contract
                try
                {
                    _loggingService.LogInfo($"Start credit check for contract [{contract.Id}]");
                    var creditCheckAlerts = new List<Alert>();
                    var checkResult = _creditCheckService.ContractCreditCheck(contract.Id, dealerId);
                    if (checkResult != null)
                    {
                        creditCheckAlerts.AddRange(checkResult.Item2);
                    }
                    //just log all credit check errors
                    if (creditCheckAlerts.Any())
                    {
                        creditCheckAlerts.Where(a => a.Type == AlertType.Error).ForEach(a =>
                            _loggingService.LogError($"Credit check error- {a.Header}: {a.Message}"));
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error during credit check for contract [{contract.Id}]", ex);
                }

                // mark as created by customer
                contract.IsCreatedByCustomer = true;
                contract.IsNewlyCreated = true;
                contract.CreateOperator = null;
                _unitOfWork.Save();


                //added comments at the end of CreateContractByCustomerFormData
                if (!string.IsNullOrEmpty(customerFormData.SelectedService) ||
                    !string.IsNullOrEmpty(customerFormData.CustomerComment))
                {
                    var addCommentsAlerts = AddCustomerComments(contract, customerFormData.SelectedService, customerFormData.CustomerComment,
                        dealerId);
                    if (addCommentsAlerts?.Any() == true)
                    {
                        alerts.AddRange(addCommentsAlerts);
                    }
                }

                submitResult = GetCustomerContractInfo(contract.Id, customerFormData.DealerName);

                if (_contractRepository.IsContractUnassignable(contract.Id))
                {
                    Task.Run(async () => await _mailService.SendNotifyMailNoDealerAcceptLead(contract));
                }
            }
            else
            {
                var errorMsg = $"Cannot get dealer {customerFormData.DealerName} from database";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.CantGetUserFromDb,
                    Message = errorMsg,
                    Header = "Cannot get dealer from database"
                });
                _loggingService.LogError(errorMsg);
            }
            if (alerts.Any(a => a.Type == AlertType.Error))
            {
                _loggingService.LogError("Cannot create contract by customer loan form request");
            }
            return new Tuple<CustomerContractInfoDTO, IList<Alert>>(submitResult, alerts);
        }

        private IList<Alert> AddCustomerComments(Contract contract, string selectedService, string customerComment, string dealerId)
        {
            var alerts = new List<Alert>();

            try
            {
                bool addedComment = false;
                if (!string.IsNullOrEmpty(selectedService))
                {
                    var serviceComment = new Comment()
                    {
                        ContractId = contract.Id,
                        Contract = contract,
                        IsCustomerComment = true,
                        Text = selectedService
                    };
                    _contractRepository.TryAddComment(serviceComment, dealerId);
                    addedComment = true;
                }
                if (!string.IsNullOrEmpty(customerComment))
                {
                    var comment = new Comment()
                    {
                        ContractId = contract.Id,
                        Contract = contract,
                        IsCustomerComment = true,
                        Text = customerComment
                    };
                    _contractRepository.TryAddComment(comment, dealerId);
                    addedComment = true;
                }
                if (addedComment)
                {
                    _unitOfWork.Save();
                    _loggingService.LogInfo($"Customer's comments is added to [{contract.Id}]");
                }
            }
            catch (Exception ex)
            {
                var errorMsg =
                    $"Cannot update contract {contract.Id} from customer loan form with customer form data";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Warning, //?
                    Code = ErrorCodes.ContractCreateFailed,
                    Header = "Cannot update contract",
                    Message = errorMsg
                });
                _loggingService.LogWarning(errorMsg);
            }

            return alerts;
        }


        private async Task SendCustomerFormContractCreationNotifications(CustomerFormDTO customerFormData, CustomerContractInfoDTO contractData)
        {
            var dealerColor = _settingsRepository.GetUserStringSettings(customerFormData.DealerName)
                                    .FirstOrDefault(s => s.Item.Name == "@navbar-header");
            var dealerLogo = _settingsRepository.GetUserBinarySetting(SettingType.LogoImage2X,
                customerFormData.DealerName);

            try
            {
                var dealerProvince = _aspireStorageReader.GetDealerInfo(customerFormData.DealerName).State;
                await _mailService.SendDealerLoanFormContractCreationNotification(customerFormData, contractData, dealerProvince).ConfigureAwait(false); 
            }
            catch (Exception ex)
            {
                var errorMsg = "Can't send dealer notification email";
                _loggingService.LogError(errorMsg, ex);
            }
            
            bool customerEmailNotification;
            bool.TryParse(_configuration.GetSetting(WebConfigKeys.CUSTOMER_EMAIL_NOFIFICATION_ENABLED_CONFIG_KEY),
                out customerEmailNotification);
            if (customerEmailNotification)
            {
                try
                {
                    await
                        _mailService.SendCustomerLoanFormContractCreationNotification(
                            customerFormData.PrimaryCustomer.Emails.FirstOrDefault(
                                m => m.EmailType == EmailType.Main)?.EmailAddress, contractData, dealerColor?.StringValue, dealerLogo?.BinaryValue).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var errorMsg = "Can't send customer notification email";
                    _loggingService.LogError(errorMsg, ex);
                }
            }
        }

        private Contract InitialyzeContract(string contractOwnerId, CustomerDTO primaryCustomer, DateTime? preferredStartDate, int? contractId = null, string equipmentType = null, 
            string customerComment = null, string leadSource = null, ContractorDTO contractor = null)
        {
            Contract contract = null;
            //update or create a brand new contract
            if (contractId.HasValue)
            {
                contract = _contractRepository.GetContract(contractId.Value, contractOwnerId);
                if (contract != null && (contract.PrimaryCustomer.AccountId == primaryCustomer.AccountId || (contract.PrimaryCustomer.Emails?.Any(e => e.EmailAddress == primaryCustomer.Emails?.FirstOrDefault()?.EmailAddress) == true)))
                {
                    _loggingService.LogInfo($"Selected contract [{contract.Id}] for update by customer loan form request for {contractOwnerId} dealer");
                }
                else
                {
                    contract = null;
                }
            }
            if (contract == null)
            {
                contract = _contractRepository.CreateContract(contractOwnerId);                
                _unitOfWork.Save();
                _loggingService.LogInfo($"Created new contract [{contract.Id}] by customer loan form request for {contractOwnerId} dealer");
            }            

            if (contract != null)
            {
                //add customer comments                
                if (!string.IsNullOrEmpty(customerComment))
                {                    
                    var commentsChanged = false;
                    //remove previously created MB comments
                    if (contract.Comments?.Any(c => c.IsCustomerComment == true) == true)
                    {
                        var commentsForRemove = contract.Comments?.Where(c => c.IsCustomerComment == true).ToList();
                        commentsForRemove.ForEach(c => _contractRepository.RemoveComment(c.Id, contractOwnerId));
                        commentsChanged = true;
                    }

                    var comment = new Comment()
                    {
                        Contract = contract,
                        IsCustomerComment = true,
                        Text = customerComment
                    };
                    _contractRepository.TryAddComment(comment, contractOwnerId);
                    if (commentsChanged)
                    {
                        _unitOfWork.Save();
                    }
                }                

                var contractDataDto = new ContractDataDTO()
                {
                    PrimaryCustomer = primaryCustomer,                    
                    DealerId = contractOwnerId,
                    Id = contract.Id,
                    Equipment = !string.IsNullOrEmpty(equipmentType)
                        ? new EquipmentInfoDTO()
                        {
                            PreferredStartDate = preferredStartDate,
                            NewEquipment =
                                new List<NewEquipmentDTO> {new NewEquipmentDTO()
                                {
                                    Type = equipmentType,
                                    Description = _contractRepository.GetEquipmentTypeInfo(equipmentType)?.Description
                                }}
                        }
                        : null,
                    LeadSource = leadSource
                };
                _contractService.UpdateContractData(contractDataDto, contractOwnerId, contractor);
            }

            return contract;
        }
    }
}
