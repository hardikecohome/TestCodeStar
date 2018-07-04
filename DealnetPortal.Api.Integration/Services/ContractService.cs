using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Utility;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Api.Models.Storage;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services
{
    using Models.Contract.EquipmentInformation;

    public partial class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly IRateCardsRepository _rateCardsRepository;
        private readonly ILoggingService _loggingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAspireService _aspireService;
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly ICreditCheckService _creditCheckService;
        //private readonly ICustomerWalletService _customerWalletService;
        private readonly IMailService _mailService;
        private readonly IAppConfiguration _configuration;
        private readonly IDocumentService _documentService;

        public ContractService(
            IContractRepository contractRepository, 
            IUnitOfWork unitOfWork, 
            IAspireService aspireService,
            IAspireStorageReader aspireStorageReader,            
            ICreditCheckService creditCheckService,
            IMailService mailService, 
            ILoggingService loggingService, IDealerRepository dealerRepository,
            IAppConfiguration configuration, IDocumentService documentService,
            IRateCardsRepository rateCardsRepository)
        {
            _contractRepository = contractRepository;
            _loggingService = loggingService;
            _dealerRepository = dealerRepository;
            _unitOfWork = unitOfWork;
            _creditCheckService = creditCheckService;
            _aspireService = aspireService;
            _aspireStorageReader = aspireStorageReader;            
            _mailService = mailService;
            _configuration = configuration;
            _documentService = documentService;
            _rateCardsRepository = rateCardsRepository;
        }

        public ContractDTO CreateContract(string contractOwnerId)
        {
            try
            {
                var newContract = _contractRepository.CreateContract(contractOwnerId);
                if (newContract != null)
                {
                    _unitOfWork.Save();
                    var contractDTO = Mapper.Map<ContractDTO>(newContract);
                    _loggingService.LogInfo($"A new contract [{newContract.Id}] created by user [{contractOwnerId}]");
                    return contractDTO;
                }
                else
                {
                    _loggingService.LogError($"Failed to create a new contract for a user [{contractOwnerId}]");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to create a new contract for a user [{contractOwnerId}]", ex);
                throw;
            }
        }

        public IList<ContractDTO> GetContracts(string contractOwnerId)
        {
            var contractDTOs = new List<ContractDTO>();
            var contracts = _contractRepository.GetContracts(contractOwnerId);

            var aspireDeals = GetAspireDealsForDealer(contractOwnerId);                        
            if (aspireDeals?.Any() ?? false)
            {
                var isContractsUpdated = UpdateContractsByAspireDeals(contracts, aspireDeals);

                var unlinkedDeals = aspireDeals.Where(ad => ad.Details?.TransactionId != null &&
                                                            contracts.All(
                                                                c =>
                                                                    (!c.Details?.TransactionId?.Contains(
                                                                        ad.Details.TransactionId) ?? true))).ToList();
                if (unlinkedDeals.Any())
                {
                    contractDTOs.AddRange(unlinkedDeals);
                }
                if (isContractsUpdated)
                {
                    try
                    {
                        _unitOfWork.Save();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError("Cannot update Aspire deals status", ex);
                    }
                }
            }

            var mappedContracts = Mapper.Map<IList<ContractDTO>>(contracts);
            AftermapContracts(contracts, mappedContracts, contractOwnerId);
            contractDTOs.AddRange(mappedContracts);

            return contractDTOs;
        }

        public int GetCustomersContractsCount(string contractOwnerId)
        {
            return _contractRepository.GetNewlyCreatedCustomersContractsCount(contractOwnerId);
        }

        public IList<ContractDTO> GetContracts(IEnumerable<int> ids, string ownerUserId)
        {
            var contracts = _contractRepository.GetContracts(ids, ownerUserId);
            var contractDTOs = Mapper.Map<IList<ContractDTO>>(contracts);
            AftermapContracts(contracts, contractDTOs, ownerUserId);
            return contractDTOs;
        }

        public ContractDTO GetContract(int contractId, string contractOwnerId)
        {
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            var beaconUpdated = false;
            //check credit report status and update if needed
            if ( (contract.Dealer?.Tier?.IsCustomerRisk == true || string.IsNullOrEmpty(contract.Dealer?.LeaseTier)) &&
                contract.ContractState > ContractState.CustomerInfoInputted &&
                contract.ContractState < ContractState.Closed &&
                contract.PrimaryCustomer != null &&                 
                contract.PrimaryCustomer.CreditReport == null)
            {
                var creditReport = _creditCheckService.CheckCustomerCreditReport(contractId, contractOwnerId);
                beaconUpdated = creditReport?.BeaconUpdated ?? false;
                var beacon = creditReport?.Beacon;
                if (beacon.HasValue)
                {
                    contract.Details.CreditAmount = 
                        contract.Details.Status.Contains("Approved") && 
                        (contract.Details.CreditAmount == null || contract.Details.CreditAmount < creditReport.CreditAmount) ? 
                        creditReport.CreditAmount : contract.Details.CreditAmount;
                    _unitOfWork.Save();
                }

            }
            

            //check contract signature status (for old contracts)
            if (contract != null && !string.IsNullOrEmpty(contract.Details?.SignatureTransactionId) &&
                (contract.Signers?.Any() == false || string.IsNullOrEmpty(contract.Details.SignatureStatusQualifier)))
            {
                _documentService.SyncSignatureStatus(contractId, contractOwnerId).GetAwaiter().GetResult();
            }

            var contractDTO = Mapper.Map<ContractDTO>(contract);
            if (contractDTO != null)
            {
                AftermapNewEquipment(contractDTO.Equipment?.NewEquipment, _contractRepository.GetEquipmentTypes());
                if (contractDTO.PrimaryCustomer?.CreditReport != null)
                {
                    // here is just aftermapping for get credit amount and escalation limits for customers who has credit report
                    contractDTO.PrimaryCustomer.CreditReport =
                        _creditCheckService.CheckCustomerCreditReport(contractId, contractOwnerId);
                    contractDTO.PrimaryCustomer.CreditReport.BeaconUpdated = beaconUpdated;
                }
                else
                {
	                var lowestCreditScoreValue = 0;
	                var creditAmountSettings = _rateCardsRepository.GetCreditAmountSetting(lowestCreditScoreValue);
	                if (contractDTO.PrimaryCustomer != null)
	                {
		                contractDTO.PrimaryCustomer.CreditReport = new CustomerCreditReportDTO
		                {
                            CreditAmount = creditAmountSettings.CreditAmount,
			                EscalatedLimit = creditAmountSettings.EscalatedLimit,
			                NonEscalatedLimit = creditAmountSettings.NonEscalatedLimit
		                };
	                }
                }
            }
            return contractDTO;
        }

        public IList<Alert> UpdateContractData(ContractDataDTO contract, string contractOwnerId, ContractorDTO contractor = null)
        {
            var alerts = new List<Alert>();
            try
            {
                var contractData = Mapper.Map<ContractData>(contract);
                var lastUpdateTime = _contractRepository.GetContract(contract.Id, contractOwnerId)?.LastUpdateTime;
                var updatedContract = _contractRepository.UpdateContractData(contractData, contractOwnerId);
                if (updatedContract != null)
                {
                    _unitOfWork.Save();
                    _loggingService.LogInfo($"A contract [{contract.Id}] updated");
                    var contractUpdated = updatedContract.LastUpdateTime > lastUpdateTime || string.IsNullOrEmpty(updatedContract.Details?.TransactionId);

                    if (contractUpdated && (contract.PrimaryCustomer != null || contract.SecondaryCustomers != null))
                    {
                        var aspireAlerts = 
                            _aspireService.UpdateContractCustomer(updatedContract, contractOwnerId, contract.LeadSource).GetAwaiter().GetResult();
                    }
                    if (contractUpdated && updatedContract.ContractState != ContractState.Completed &&
                        updatedContract.ContractState != ContractState.Closed && !updatedContract.DateOfSubmit.HasValue)
                    {
                        var aspireAlerts = 
                            _aspireService.SendDealUDFs(updatedContract, contractOwnerId, contract.LeadSource, contractor).GetAwaiter().GetResult();
                    }
                    else if (contractUpdated && (updatedContract.ContractState == ContractState.Completed || updatedContract.DateOfSubmit.HasValue))
                    {
                        //if Contract has been submitted already, we will resubmit it to Aspire after each contract changes 
                        //(DEAL-3628: [DP] Submit deal after each step when editing previously submitted deal)
                        var submitResTask =
                            Task.Run(async () =>
                                await ReSubmitContract(contract.Id, contractOwnerId));
                        var submitRes = submitResTask.GetAwaiter().GetResult();
                        if (submitRes?.Any() == true)
                        {
                            alerts.AddRange(submitRes);
                        }
                    }                    
                }
                else
                {
                    var errorMsg =
                        $"Cannot find a contract [{contract.Id}] for update. Contract owner: [{contractOwnerId}]";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ContractUpdateFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to update a contract [{contract.Id}]", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.ContractUpdateFailed,
                    Message = $"Failed to update a contract [{contract.Id}]"
                });
            }
            return alerts;
        }

        public IList<Alert> NotifyContractEdit(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                //Remove newly created by customer mark, if contract is opened for edit
                try
                {
                    contract.IsNewlyCreated = false;
                    contract.LastUpdateTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(contract.Dealer?.UserName))
                    {
                        contract.LastUpdateOperator = contract.Dealer?.UserName;
                    }
                    _unitOfWork.Save();
                }
                catch (Exception ex)
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ContractUpdateFailed,
                        Code = ErrorCodes.FailedToUpdateContract,
                        Message = $"Cannot update contract [{contractId}]"
                    });
                    _loggingService.LogError($"Cannot update contract [{contractId}]", ex);
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Cannot find contract",
                    Message = $"Cannot find contract [{contractId}] for update"
                });
            }
            return alerts;
        }

        public AgreementDocument GetContractsFileReport(IEnumerable<int> ids, string contractOwnerId, int? timeZoneOffset = null)
        {
            var stream = new MemoryStream();
            var contracts =
                Mapper.Map<IList<ContractDTO>>(_contractRepository.GetContractsEquipmentInfo(ids, contractOwnerId));                
            var provincialTaxRates = _contractRepository.GetAllProvinceTaxRates().ToList();

            var equipmentTypes = _contractRepository.GetEquipmentTypes();
            foreach (var contract in contracts)
            {
                AftermapNewEquipment(contract.Equipment?.NewEquipment, equipmentTypes);
            }

            XlsxExporter.Export(contracts, stream, provincialTaxRates);
            var report = new AgreementDocument()
            {
                DocumentRaw = stream.ToArray(),
                Name = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture).Replace(":", ".")}-report.xlsx",
            };
            return report;
        }

        public IList<Alert> UpdateInstallationData(InstallationCertificateDataDTO installationCertificateData,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                var contract = _contractRepository.GetContract(installationCertificateData.ContractId, contractOwnerId);

                if (contract != null)
                {
                    if (contract.Equipment != null)
                    {
                        if (!string.IsNullOrEmpty(installationCertificateData.InstallerFirstName))
                        {
                            contract.Equipment.InstallerFirstName = installationCertificateData.InstallerFirstName;
                        }
                        if (!string.IsNullOrEmpty(installationCertificateData.InstallerLastName))
                        {
                            contract.Equipment.InstallerLastName = installationCertificateData.InstallerLastName;
                        }
                        if (installationCertificateData.InstallationDate.HasValue)
                        {
                            contract.Equipment.InstallationDate = installationCertificateData.InstallationDate;
                        }
                        if (installationCertificateData.InstalledEquipments?.Any() ?? false)
                        {
                            installationCertificateData.InstalledEquipments.ForEach(ie =>
                            {
                                var eq = contract.Equipment.NewEquipment.FirstOrDefault(e => e.Id == ie.Id);
                                if (eq != null)
                                {
                                    if (!string.IsNullOrEmpty(ie.Model))
                                    {
                                        eq.InstalledModel = ie.Model;
                                    }
                                    if (!string.IsNullOrEmpty(ie.SerialNumber))
                                    {
                                        eq.InstalledSerialNumber = ie.SerialNumber;
                                    }
                                }
                            });
                        }
                    }
                    _unitOfWork.Save();
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.CreditCheckFailed,
                        Message = "Cannot find a contract [{contractId}] for initiate credit check"
                    });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    $"Cannot update installation data for contract {installationCertificateData.ContractId}", ex);
            }
            return alerts;
        }

        public Tuple<CreditCheckDTO, IList<Alert>> SubmitContract(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();
            CreditCheckDTO creditCheck = null;

            var aspireAlerts = _aspireService.SubmitDeal(contractId, contractOwnerId).GetAwaiter().GetResult();
            if (aspireAlerts?.Any() ?? false)
            {
                alerts.AddRange(aspireAlerts);
            }

            if (aspireAlerts?.All(ae => ae.Type != AlertType.Error) ?? false)
            {
                var creditCheckRes =
                    _aspireService.InitiateCreditCheck(contractId, contractOwnerId).GetAwaiter().GetResult();
                if (creditCheckRes?.Item2?.Any() ?? false)
                {
                    alerts.AddRange(creditCheckRes.Item2);
                }
                if (alerts.All(a => a.Type != AlertType.Error) && creditCheckRes?.Item1 != null)
                {
                    creditCheck = creditCheckRes?.Item1;
                    Contract contract = null;
                    switch (creditCheckRes?.Item1.CreditCheckState)
                    {
                        case CreditCheckState.Declined:
                            contract = _contractRepository.UpdateContractState(contractId, contractOwnerId,
                                ContractState.CreditCheckDeclined);
                            break;
                        default:
                            var aspireStatus = _contractRepository.GetAspireStatus(_contractRepository.GetContract(contractId).Details?.Status);
                            contract = (aspireStatus != null && aspireStatus.ContractState == ContractState.Closed) ? _contractRepository.UpdateContractState(contractId, contractOwnerId, ContractState.Closed)
                                : _contractRepository.UpdateContractState(contractId, contractOwnerId, ContractState.Completed);
                            break;
                    }
                    contract = _contractRepository.UpdateContractAspireSubmittedDate(contractId, contractOwnerId);
                    //var contract = _contractRepository.UpdateContractState(contractId, contractOwnerId,
                    //    ContractState.Completed);
                    if (contract != null)
                    {
                        _unitOfWork.Save();
                        var submitState = creditCheckRes.Item1.CreditCheckState == CreditCheckState.Declined
                            ? "declined"
                            : "submitted";
                        _loggingService.LogInfo($"Contract [{contractId}] {submitState}");
                    }
                    else
                    {
                        var errorMsg = $"Cannot submit contract [{contractId}]";
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = ErrorConstants.SubmitFailed,
                            Message = errorMsg
                        });
                        _loggingService.LogError(errorMsg);
                    }
                }
            }
            return new Tuple<CreditCheckDTO, IList<Alert>>(creditCheck, alerts);
        }

        public IList<FlowingSummaryItemDTO> GetDealsFlowingSummary(string contractsOwnerId,
            FlowingSummaryType summaryType)
        {
            IList<FlowingSummaryItemDTO> summary = new List<FlowingSummaryItemDTO>();
            var dealerContracts = _contractRepository.GetContracts(contractsOwnerId);

            if (dealerContracts?.Any() ?? false)
            {
                switch (summaryType)
                {
                    case FlowingSummaryType.Month:
                        var firstMonthDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        var grDaysM =
                            dealerContracts.Where(c => c.CreationTime >= firstMonthDate)
                                .GroupBy(c => c.CreationTime.Day)
                                .ToList();

                        for (var i = 1; i <= DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month); i++)
                        {
                            var contractsG = grDaysM.FirstOrDefault(g => g.Key == i);
                            double totalSum = 0;
                            contractsG?.ForEach(c =>
                            {
                                //var totalMp = _contractRepository.GetContractPaymentsSummary(c.Id);
                                //totalSum += totalMp?.TotalAllMonthlyPayment ?? 0;
                                totalSum += (double)(c.Equipment?.ValueOfDeal ?? 0);
                            });
                            summary.Add(new FlowingSummaryItemDTO()
                            {
                                ItemLabel = i.ToString(),
                                ItemCount = contractsG?.Count() ?? 0, //grDaysM.Count(g => g.Key == i),
                                ItemData = totalSum // rand.Next(1, 100)
                            });
                        }
                        break;
                    case FlowingSummaryType.Week:
                        var weekDays = DateTimeFormatInfo.CurrentInfo.DayNames;
                        var curDayIdx = Array.IndexOf(weekDays,
                            Thread.CurrentThread.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek));
                        var daysDiff = -curDayIdx;
                        var grDays =
                            dealerContracts.Where(c => c.CreationTime >= DateTime.Today.AddDays(daysDiff))
                                .GroupBy(
                                    c =>
                                        Thread.CurrentThread.CurrentCulture.DateTimeFormat.GetDayName(
                                            c.CreationTime.DayOfWeek))
                                .ToList();

                        for (int i = 0; i < weekDays.Length; i++)
                        {
                            var contractsW = grDays.FirstOrDefault(g => g.Key == weekDays[i]);
                            double totalSum = 0;
                            contractsW?.ForEach(c =>
                            {
                                //var totalMp = _contractRepository.GetContractPaymentsSummary(c.Id);
                                //totalSum += totalMp?.TotalAllMonthlyPayment ?? 0;
                                totalSum += (double)(c.Equipment?.ValueOfDeal ?? 0);
                            });

                            summary.Add(new FlowingSummaryItemDTO()
                            {
                                ItemLabel = weekDays[i],
                                ItemCount = contractsW?.Count() ?? 0,
                                ItemData = totalSum //rand.Next(1, 100)
                            });
                        }
                        break;
                    case FlowingSummaryType.Year:
                        var months = DateTimeFormatInfo.CurrentInfo.MonthNames;
                        var grMonths =
                            dealerContracts.Where(c => c.CreationTime.Year == DateTime.Today.Year)
                                .GroupBy(c => c.CreationTime.Month)
                                .ToList();

                        for (int i = 0; i < months.Length; i++)
                        {
                            var contractsM = grMonths.FirstOrDefault(g => g.Key == i + 1);
                            double totalSum = 0;
                            contractsM?.ForEach(c =>
                            {
                                //var totalMp = _contractRepository.GetContractPaymentsSummary(c.Id);
                                //totalSum += totalMp?.TotalAllMonthlyPayment ?? 0;
                                totalSum += (double)(c.Equipment?.ValueOfDeal ?? 0);
                            });

                            summary.Add(new FlowingSummaryItemDTO()
                            {
                                ItemLabel = DateTimeFormatInfo.CurrentInfo.MonthNames[i],
                                ItemCount = contractsM?.Count() ?? 0,
                                ItemData = totalSum
                            });
                        }
                        break;
                }
            }

            return summary;
        }

        public Tuple<IList<EquipmentTypeDTO>, IList<Alert>> GetDealerEquipmentTypes(string dealerId)
        {
            var alerts = new List<Alert>();
            try
            {
                var dealerProfile = _dealerRepository.GetDealerProfile(dealerId);
                IList<EquipmentType> equipmentTypes;
                if (dealerProfile != null && dealerProfile.Equipments.Any())
                {
                    equipmentTypes = dealerProfile.Equipments.Select(x=>x.Equipment).ToList();
                }
                else
                {
                    equipmentTypes = _contractRepository.GetEquipmentTypes();
                }
                
                var equipmentTypeDtos = Mapper.Map<IList<EquipmentTypeDTO>>(equipmentTypes);
                if (!equipmentTypes.Any())
                {
                    var errorMsg = "Cannot retrieve Equipment Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
                return new Tuple<IList<EquipmentTypeDTO>, IList<Alert>>(equipmentTypeDtos, alerts);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to retrieve Equipment Types", ex);
                throw;
            }
        }

        public Tuple<ProvinceTaxRateDTO, IList<Alert>> GetProvinceTaxRate(string province)
        {
            var alerts = new List<Alert>();
            try
            {
                var provinceTaxRate = _contractRepository.GetProvinceTaxRate(province);
                var provinceTaxRateDto = Mapper.Map<ProvinceTaxRateDTO>(provinceTaxRate);
                if (provinceTaxRate == null)
                {
                    var errorMsg = "Cannot retrieve Province Tax Rate";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ProvinceTaxRateRetrievalFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
                return new Tuple<ProvinceTaxRateDTO, IList<Alert>>(provinceTaxRateDto, alerts);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to retrieve Province Tax Rate", ex);
                throw;
            }
        }        

        public CustomerDTO GetCustomer(int customerId)
        {
            var customer = _contractRepository.GetCustomer(customerId);
            return Mapper.Map<CustomerDTO>(customer);
        }

        public IList<Alert> UpdateCustomers(CustomerDataDTO[] customers, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                // update only new customers for declined deals
                if (customers?.Any() == true)
                {
                    var contractId = customers.FirstOrDefault(c => c.ContractId.HasValue)?.ContractId;
                    var contract = contractId.HasValue
                        ? _contractRepository.GetContractAsUntracked(contractId.Value, contractOwnerId)
                        : null;

                    var customersUpdated = false;
                    var customersMailsUpdated = false;

                    customers.ForEach(c =>
                    {
                        if (contract == null || contract.WasDeclined != true ||
                            (contract.InitialCustomers?.All(ic => ic.Id != c.Id) ?? false))
                        {
                            if (c.CustomerInfo == null && c.Locations?.Any() != true &&
                                c.Phones?.Any() != true &&
                                c.Emails?.Any() == true)
                            {
                                var mails = Mapper.Map<IList<Email>>(c.Emails);
                                mails.ForEach(email =>
                                {
                                    if (email.EmailType == EmailType.Main)
                                    {
                                        customersUpdated |= _contractRepository.UpdateCustomerEmails(c.Id,
                                            new List<Email>(){ email });
                                    }
                                    else
                                    {
                                        customersMailsUpdated |= _contractRepository.UpdateCustomerEmails(c.Id,
                                            new List<Email>() { email });
                                    }
                                });                                
                            }
                            else
                            {
                                customersUpdated |= _contractRepository.UpdateCustomerData(c.Id,
                                    Mapper.Map<Customer>(c.CustomerInfo),
                                    Mapper.Map<IList<Location>>(c.Locations), Mapper.Map<IList<Phone>>(c.Phones),
                                    Mapper.Map<IList<Email>>(c.Emails));
                            }
                        }
                    });

                    if (customersUpdated || customersMailsUpdated)
                    {
                        _unitOfWork.Save();
                    }

                    if (customersUpdated == true)
                    {                                            
                        // get latest contract changes
                        if (contractId.HasValue)
                        {
                            contract = _contractRepository.GetContractAsUntracked(contractId.Value,
                                contractOwnerId);
                        }
                        if (contract != null)
                        {
                            // update customers on aspire
                            var leadSource = customers.FirstOrDefault(c => !string.IsNullOrEmpty(c.LeadSource))
                                ?.LeadSource;
                            _aspireService.UpdateContractCustomer(contractId.Value, contractOwnerId, leadSource);

                            if (contract.ContractState == ContractState.Completed ||
                                contract.DateOfSubmit.HasValue)
                            {
                                var submitResTask =
                                    Task.Run(async () =>
                                        await ReSubmitContract(contract.Id, contractOwnerId));
                                var submitRes = submitResTask.GetAwaiter().GetResult();
                                if (submitRes?.Any() == true)
                                {
                                    alerts.AddRange(submitRes);
                                }
                            }
                        }                       
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to update customers data", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Failed to update customers data",
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public Tuple<int?, IList<Alert>> AddComment(CommentDTO commentDTO, string contractOwnerId)
        {
            var alerts = new List<Alert>();
            Comment comment = null;

            try
            {
                comment = _contractRepository.TryAddComment(Mapper.Map<Comment>(commentDTO), contractOwnerId);
                if (comment != null)
                {
                    _unitOfWork.Save();
                    //don't send mails for Customer Comment, as we usually add these comments on contract creation (from CW or Shareble link)
                }
                else
                {
                    var errorMsg = "Cannot update contract comment";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.CommentUpdateFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to update contract comment", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.CommentUpdateFailed,
                    Message = ex.ToString()
                });
            }

            return new Tuple<int?, IList<Alert>>(comment?.Id, alerts);
        }

        public IList<Alert> RemoveComment(int commentId, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                var removedCommentContractId = _contractRepository.RemoveComment(commentId, contractOwnerId);
                if (removedCommentContractId != null)
                {
                    _unitOfWork.Save();
                }
                else
                {
                    var errorMsg = "Cannot update contract comment";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.CommentUpdateFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to update contract comment", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.CommentUpdateFailed,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public IList<ContractDTO> GetDealerLeads(string userId)
        {
            var contractDTOs = new List<ContractDTO>();            
            // temporary using a flag IsCreatedByBroker
            var contracts = _contractRepository.GetDealerLeads(userId);            
            var mappedContracts = Mapper.Map<IList<ContractDTO>>(contracts);
            AftermapContracts(contracts, mappedContracts, userId);
            contractDTOs.AddRange(mappedContracts);
            return contractDTOs;
        }

        private IList<ContractDTO> GetAspireDealsForDealer(string contractOwnerId)
        {
            var user = _contractRepository.GetDealer(contractOwnerId);
            if (user != null)
            {
                try
                {
                    //var deals = _aspireStorageService.GetDealerDeals(user.DisplayName);
                    var deals = Mapper.Map<IList<ContractDTO>>(_aspireStorageReader.GetDealerDeals(user.UserName));
                    if (deals?.Any() ?? false)
                    {
                        //skip deals that already in DB                        
                        var equipments = _contractRepository.GetEquipmentTypes();
                        if (equipments?.Any() ?? false)
                        {
                            deals.ForEach(d =>
                            {
                                var eqType = d.Equipment?.NewEquipment?.FirstOrDefault()?.Type;
                                if (!string.IsNullOrEmpty(eqType))
                                {
                                    var equipment = equipments.FirstOrDefault(eq => eq.Description == eqType);
                                    if (equipment != null)
                                    {
                                        d.Equipment.NewEquipment.FirstOrDefault().Type = equipment.Type;
                                        d.Equipment.NewEquipment.FirstOrDefault().TypeDescription =
                                            ResourceHelper.GetGlobalStringResource(equipment.Description);
                                    }
                                    else
                                    {
                                        d.Equipment.NewEquipment.FirstOrDefault().TypeDescription = eqType;
                                    }                                                                                                                
                                }
                            });
                        }
                    }
                    return deals;
                }                                
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error occured during get deals from aspire", ex);
                }
            }
            else
            {
                _loggingService.LogError($"Cannot get a dealer {contractOwnerId}");
            }
            return null;
        }

        public Tuple<IList<DocumentTypeDTO>, IList<Alert>> GetContractDocumentTypes(int contractId,
            string contractOwnerId)
        {
            var alerts = new List<Alert>();
            IList<DocumentTypeDTO> docTypes = null;
            try
            {
                docTypes = Mapper.Map<IList<DocumentTypeDTO>>(_contractRepository.GetContractDocumentTypes(contractId, contractOwnerId));                                
            }
            catch (Exception ex)
            {
                var errorMsg = "Cannot retrieve Document Types";
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                    Message = errorMsg
                });
                _loggingService.LogError(errorMsg, ex);
            }

            var result = new Tuple<IList<DocumentTypeDTO>, IList<Alert>>(docTypes, alerts);
            return result;
        }

        public Tuple<int?, IList<Alert>> AddDocumentToContract(ContractDocumentDTO document, string contractOwnerId)
        {
            var alerts = new List<Alert>();
            ContractDocument doc = null;
            document.DocumentName = document.DocumentName.Replace('-', '_');
            try
            {
                
                //run aspire upload async
                var aspireAlerts = _aspireService.UploadDocument(document.ContractId, document, contractOwnerId).GetAwaiter().GetResult();
                if (aspireAlerts.Any())
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "Failed to add document to contract",
                        Message = aspireAlerts.FirstOrDefault().Message
                    });
                    _loggingService.LogError(aspireAlerts.FirstOrDefault().Message);
                }
                else
                {
                    doc = _contractRepository.AddDocumentToContract(document.ContractId, Mapper.Map<ContractDocument>(document),
                    contractOwnerId);
                    _unitOfWork.Save();
                    
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to add document to contract", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error, Header = "Failed to add document to contract", Message = ex.ToString()
                });
            }
            return new Tuple<int?, IList<Alert>>(doc?.Id, alerts); ;
        }

        public IList<Alert> RemoveContractDocument(int documentId, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                if (_contractRepository.TryRemoveContractDocument(documentId, contractOwnerId))
                {
                    _unitOfWork.Save();
                }
                else
                {
                    var errorMsg = "Cannot remove contract document";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.DocumentUpdateFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to remove contract document", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.DocumentUpdateFailed,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public async Task<IList<Alert>> SubmitAllDocumentsUploaded(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                if (IsSentToAuditValid(contract))
                {
                    try
                    {
                        // if e-signature is initiated, cancel e-signature
                        if (!string.IsNullOrEmpty(contract.Details.SignatureTransactionId) &&
                            (contract.Details.SignatureStatus == SignatureStatus.Created || contract.Details.SignatureStatus == SignatureStatus.Sent
                                || contract.Details.SignatureStatus == SignatureStatus.Delivered))
                        {
                            var cancelResults = await _documentService.CancelSignatureProcess(contractId, contractOwnerId);
                            if (cancelResults?.Item2?.Any() == true)
                            {
                                alerts.AddRange(cancelResults.Item2);
                            }
                        }

                        var status = _configuration.GetSetting(WebConfigKeys.ALL_DOCUMENTS_UPLOAD_STATUS_CONFIG_KEY);
                        var aspireAlerts = await _aspireService.ChangeDealStatus(contract.Details?.TransactionId, status, contractOwnerId, "Request to Fund");
                        //var aspireAlerts = await _aspireService.SubmitAllDocumentsUploaded(contractId, contractOwnerId);
                        if (aspireAlerts?.Any() ?? false)
                        {
                            alerts.AddRange(aspireAlerts);
                        }

                        if (alerts.All(a => a.Type != AlertType.Error))
                        {
                            contract.ContractState = ContractState.Closed;
                            _unitOfWork.Save();
                            _loggingService.LogInfo(
                                $"Request to Audit was successfully sent to Aspire for contract {contractId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError("Failed to submit All Documents Uploaded Request", ex);
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "Failed to submit All Documents Uploaded Request",
                            Message = ex.ToString()
                        });
                    }
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Header = "Not all mandatory documents were uploaded",
                        Message = $"Not all mandatory documents were uploaded for contract with id {contractId}",
                        Type = AlertType.Error
                    });
                    _loggingService.LogError($"Not all mandatory documents were uploaded for contract with id {contractId}");
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Code = ErrorCodes.CantGetContractFromDb,
                    Header = "Cannot find contract",
                    Message = $"Cannot find contract [{contractId}] for update"
                });
            }                        
            return alerts;
        }        

        public IList<Alert> RemoveContract(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                if (_contractRepository.DeleteContract(contractOwnerId, contractId))
                {
                    _unitOfWork.Save();
                }
                else
                {
                    var errorMsg = "Cannot remove contract";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ContractRemoveFailed,
                        Message = errorMsg
                    });
                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to remove contract", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.DocumentUpdateFailed,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public async Task<IList<Alert>> AssignContract(int contractId, string newContractOwnerId)
        {
            var alerts = new List<Alert>();

            try
            {
                //move installation address to main address for MB contracts reassign

                var updatedContract = _contractRepository.AssignContract(contractId, newContractOwnerId);
                if (updatedContract != null)
                {
                    _unitOfWork.Save();
                    var dealer = Mapper.Map<DealerDTO>(_aspireStorageReader.GetDealerInfo(updatedContract.Dealer.UserName));
                    await _mailService.SendCustomerDealerAcceptLead(updatedContract, dealer);
                    await  _aspireService.UpdateContractCustomer(contractId, newContractOwnerId);
                }
                else
                {
                    var errorMsg = Resources.Resources.UnfortunatelyThisLeadIsNoLongerAvailable;
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ContractUpdateFailed,
                        Message = errorMsg
                    });

                    _loggingService.LogError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Failed to assign contract", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.DocumentUpdateFailed,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        private bool UpdateContractsByAspireDeals(IList<Contract> contractsForUpdate, IList<ContractDTO> aspireDeals)
        {
            bool isChanged = false;
            foreach (var aspireDeal in aspireDeals)
            {
                if (aspireDeal.Details?.TransactionId == null)
                {
                    continue;
                }
                var contract =
                    contractsForUpdate.FirstOrDefault(
                        c => (c.Details?.TransactionId?.Contains(aspireDeal.Details.TransactionId) ?? false));
                if (contract != null)
                {
                    if (contract.Details.Status != aspireDeal.Details.Status)
                    {
                        contract.Details.Status = aspireDeal.Details.Status;
                        contract.LastUpdateTime = DateTime.UtcNow;
                        isChanged = true;
                    }
                    if (aspireDeal.Details?.CreditAmount != null)
                    {
                        if (contract.Details.CreditAmount == null || contract.Details.CreditAmount != aspireDeal.Details.CreditAmount) {
                            contract.Details.CreditAmount = aspireDeal.Details.CreditAmount;
                            isChanged = true;
                        }
                    }
                    if (!string.IsNullOrEmpty(aspireDeal.Details?.OverrideCustomerRiskGroup))
                    {
                        contract.Details.OverrideCustomerRiskGroup = aspireDeal.Details.OverrideCustomerRiskGroup;
                        isChanged = true;
                    }
                    //update contract state in any case
                    isChanged |= UpdateContractState(contract);
                }
            }
            return isChanged;
        }

        /// <summary>
        /// Logic for update internal contract state by Aspire state
        /// </summary>
        /// <param name="contract"></param>
        private bool UpdateContractState(Contract contract)
        {
            bool isChanged = false;
            var aspireStatus = _contractRepository.GetAspireStatus(contract.Details?.Status);
            if (aspireStatus?.ContractState != null)
            {
                if (contract.ContractState != aspireStatus.ContractState)
                {
                    contract.ContractState = aspireStatus.ContractState.Value;
                    contract.LastUpdateTime = DateTime.UtcNow;
                    isChanged = true;
                }
                switch (aspireStatus.ContractState)
                {
                    case ContractState.CreditCheckDeclined:
                        contract.WasDeclined = true;
                        break;
                }
            }
            else
            {
                // if current status is Closed reset it to Completed
                if (contract.ContractState == ContractState.Closed)
                {
                    contract.ContractState = ContractState.Completed;
                    contract.LastUpdateTime = DateTime.UtcNow;
                    isChanged = true;
                }
            }
            return isChanged;
        }

        private async Task<IList<Alert>> ReSubmitContract(int contractId, string contractOwnerId)
        {
            var alerts = new List<Alert>();            
            var submitRes = SubmitContract(contractId, contractOwnerId);            
            //check contract signature status and clean if needed
            if (submitRes != null)
            {
                if (submitRes.Item2?.Any() == true)
                {
                    alerts.AddRange(submitRes.Item2);
                }
                var contract = _contractRepository.GetContract(contractId, contractOwnerId);
                if (contract != null)
                {
                    if (contract.Details.SignatureStatus != null ||
                                            !string.IsNullOrEmpty(contract.Details?.SignatureTransactionId) &&
                                            contract.LastUpdateTime >
                                            contract.Details.SignatureInitiatedTime)
                    {
                        var cancelRes = await _documentService.CancelSignatureProcess(contractId, contractOwnerId, Resources.Resources.ContractChangedCancelledEsign);
                        if (cancelRes?.Item2?.Any() == true)
                        {
                            alerts.AddRange(cancelRes.Item2);
                        }
                        _documentService.CleanSignatureInfo(contractId, contractOwnerId);
                    }
                }
            }            
            return alerts;
        }

        private bool IsSentToAuditValid(Contract contract)
        {
            bool isValid = false;
            var state = contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)?.State;
            state = state ?? contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress)?.State;
            var reqDocs = _contractRepository.GetDealerDocumentTypes(state, contract.DealerId).Where(x=>x.IsMandatory);
            //required documents
            isValid |= reqDocs.All(x => contract.Documents.Any(d => d.DocumentTypeId == x.Id) || x.Id == (int)DocumentTemplateType.SignedContract);

            //signed document
            isValid &= (contract.Details?.SignatureStatus == SignatureStatus.Completed ||
                        contract.Documents?.Any(d => d.DocumentTypeId == (int)DocumentTemplateType.SignedContract) == true);

            return isValid;
        }

        private void AftermapContracts(IList<Contract> contracts, IList<ContractDTO> contractDTOs, string ownerUserId)
        {
            var equipmentTypes = _contractRepository.GetEquipmentTypes();
            foreach (var contractDTO in contractDTOs)
            {
                AftermapNewEquipment(contractDTO.Equipment?.NewEquipment, equipmentTypes);
                var contract = contracts.FirstOrDefault(x => x.Id == contractDTO.Id);
                if (contract != null) { AftermapComments(contract.Comments, contractDTO.Comments, ownerUserId); }
            }
        }

        private void AftermapNewEquipment(IList<NewEquipmentDTO> equipment, IList<EquipmentType> equipmentTypes)
        {
            equipment?.ForEach(eq => eq.TypeDescription = ResourceHelper.GetGlobalStringResource(equipmentTypes.FirstOrDefault(eqt => eqt.Type == eq.Type)?.DescriptionResource));
        }

        private void AftermapComments(IEnumerable<Comment> src, IEnumerable<CommentDTO> dest, string contractOwnerId)
        {
            var srcComments = src.ToArray();
            foreach (var destComment in dest)
            {
                var scrComment = srcComments.FirstOrDefault(x => x.Id == destComment.Id);
                if (scrComment == null)
                {
                    continue;
                }
                destComment.IsOwn = scrComment.DealerId == contractOwnerId;
                if (destComment.Replies.Any())
                {
                    AftermapComments(scrComment.Replies, destComment.Replies, contractOwnerId);
                }
            }
        }
    }
}
