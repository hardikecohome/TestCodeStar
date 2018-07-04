using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Aspire.Integration.Models.AspireDb;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using Contract = DealnetPortal.Domain.Contract;

namespace DealnetPortal.Api.Integration.Services
{
    public class CreditCheckService : ICreditCheckService
    {
        private readonly IAspireService _aspireService;
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly ILoggingService _loggingService;
        private readonly IContractRepository _contractRepository;
        private readonly IRateCardsRepository _rateCardsRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly IAppConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public CreditCheckService(IAspireService aspireService, IAspireStorageReader aspireStorageReader,
            IContractRepository contractRepository, IRateCardsRepository rateCardsRepository, IDealerRepository dealerRepository,
            IUnitOfWork unitOfWork, IAppConfiguration configuration,
            ILoggingService loggingService)
        {
            _aspireService = aspireService;
            _aspireStorageReader = aspireStorageReader;
            _contractRepository = contractRepository;
            _rateCardsRepository = rateCardsRepository;
            _dealerRepository = dealerRepository;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _loggingService = loggingService;
        }

        public Tuple<CreditCheckDTO, IList<Alert>> ContractCreditCheck(int contractId, string contractOwnerId)
        {
            CreditCheckDTO creditCheck = null;
            List<Alert> alerts = new List<Alert>();
            var contract = _contractRepository.GetContract(contractId, contractOwnerId);

            if (contract?.ContractState == null)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.CreditCheckFailed,
                    Message = "Cannot find a contract [{contractId}] for initiate credit check"
                });
            }
            else
            {
                if (contract.ContractState == ContractState.CustomerInfoInputted)
                {
                    _contractRepository.UpdateContractState(contractId, contractOwnerId,
                        ContractState.CreditCheckInitiated);
                    _unitOfWork.Save();
                }
                _loggingService.LogInfo($"Initiated credit check for contract [{contractId}]");
                var isFrenchSymbols = IsFrenchSymbols(contract);
                if (isFrenchSymbols)
                {
                    var aspireAlerts =
                        _aspireService.UpdateContractCustomer(contract, contractOwnerId, null, true)
                            .GetAwaiter()
                            .GetResult();
                    if (aspireAlerts?.Any() == true)
                    {
                        alerts.AddRange(aspireAlerts);
                    }
                }
                var checkResult = _aspireService.InitiateCreditCheck(contractId, contractOwnerId).GetAwaiter().GetResult();
                if (isFrenchSymbols)
                {
                    var aspireAlerts = _aspireService.UpdateContractCustomer(contract, contractOwnerId).GetAwaiter().GetResult();
                    if (aspireAlerts?.Any() == true)
                    {
                        alerts.AddRange(aspireAlerts);
                    }
                    checkResult = _aspireService.InitiateCreditCheck(contractId, contractOwnerId).GetAwaiter().GetResult();
                }
                creditCheck = checkResult?.Item1;
                if (checkResult?.Item2?.Any() == true)
                {
                    alerts.AddRange(checkResult.Item2);
                }
                if (checkResult?.Item1 != null)
                {
                    switch (checkResult.Item1.CreditCheckState)
                    {
                        case CreditCheckState.Approved:
                            _contractRepository.UpdateContractState(contractId, contractOwnerId,
                                ContractState.CreditConfirmed);
                            _unitOfWork.Save();
                            break;
                        case CreditCheckState.Declined:
                            _contractRepository.UpdateContractState(contractId, contractOwnerId,
                                ContractState.CreditCheckDeclined);
                            _unitOfWork.Save();
                            break;
                        case CreditCheckState.MoreInfoRequired:
                            _contractRepository.UpdateContractState(contractId, contractOwnerId,
                                ContractState.CreditConfirmed);
                            _unitOfWork.Save();
                            break;
                    }

                    var creditAmount = checkResult.Item1.CreditAmount > 0
                        ? checkResult.Item1.CreditAmount
                        : (decimal?) null;
                    var scorecardPoints = checkResult.Item1.ScorecardPoints > 0
                        ? checkResult.Item1.ScorecardPoints
                        : (int?) null;

                    var dealerRoles = _dealerRepository.GetUserRoles(contract.DealerId);
                    if (contract.Dealer?.Tier?.IsCustomerRisk == true 
                        || string.IsNullOrEmpty(contract.Dealer?.LeaseTier)
                        || dealerRoles?.Contains(UserRole.MortgageBroker.ToString()) == true
                        || dealerRoles?.Contains(UserRole.CustomerCreator.ToString()) == true)
                    {
                        var creditReport = CheckCustomerCreditReport(contractId, contractOwnerId);
                        var beacon = creditReport?.Beacon;
                        checkResult.Item1.Beacon = beacon ?? 0;
                        creditAmount = beacon.HasValue ? creditReport.CreditAmount : creditAmount;
                        if (creditAmount.HasValue)
                        {
                            checkResult.Item1.CreditAmount = creditAmount.Value;
                        }
                    }

                    if (creditAmount.HasValue || scorecardPoints.HasValue)
                    {
                        _contractRepository.UpdateContractData(new ContractData()
                        {
                            Id = contractId,
                            Details = new ContractDetails()
                            {
                                CreditAmount = creditAmount,
                                ScorecardPoints = scorecardPoints,
                                HouseSize = contract.Details.HouseSize,
                                Notes = contract.Details.Notes
                            }
                        }, contractOwnerId);
                        _unitOfWork.Save();
                    }
                }
            }

            return new Tuple<CreditCheckDTO, IList<Alert>>(creditCheck, alerts);
        }

       public CustomerCreditReportDTO CheckCustomerCreditReport(int contractId, string contractOwnerId)
        {
            CustomerCreditReportDTO creditReport = null;

            var contract = _contractRepository.GetContract(contractId, contractOwnerId);
            if (contract?.PrimaryCustomer != null)
            {
                if (contract.PrimaryCustomer.CreditReport != null)
                {
                    creditReport =
                        AutoMapper.Mapper.Map<CustomerCreditReportDTO>(contract.PrimaryCustomer.CreditReport);
                }
                else
                {
                    var useTestAspire = false;
                    bool.TryParse(_configuration.GetSetting(WebConfigKeys.USE_TEST_ASPIRE), out useTestAspire);
                    CreditReport dbCreditReport = null;
                    if (useTestAspire)
                    {
                        //check user on Aspire
                        var postalCode =
                            contract.PrimaryCustomer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)?.PostalCode ??
                            contract.PrimaryCustomer.Locations?.FirstOrDefault()?.PostalCode;
                        dbCreditReport = _aspireStorageReader.GetCustomerCreditReport(contract.PrimaryCustomer.FirstName.MapFrenchSymbols(true),
                            contract.PrimaryCustomer.LastName.MapFrenchSymbols(true),
                            contract.PrimaryCustomer.DateOfBirth, postalCode);
                    }
                    else
                    {
                        dbCreditReport = _aspireStorageReader.GetCustomerCreditReport(contract.PrimaryCustomer.AccountId.ToString());
                    }
                    if (dbCreditReport != null)
                    {
                        var customer = new Customer()
                        {
                            Id = contract.PrimaryCustomer.Id,
                            CreditReport = new CustomerCreditReport()
                            {
                                Beacon = dbCreditReport.Beacon,
                                CreditLastUpdateTime = DateTime.UtcNow
                            }
                        };
                        _contractRepository.UpdateCustomerData(customer.Id, customer, null, null, null);
                        _unitOfWork.Save();

                        creditReport = new CustomerCreditReportDTO()
                        {
                            Beacon = dbCreditReport.Beacon,
                            CreditLastUpdateTime = DateTime.UtcNow
                        };
                    }
                }
                if (creditReport != null)
                {
                    var creditAmount = _rateCardsRepository.GetCreditAmountSetting(creditReport.Beacon.Value);
                    if (creditAmount != null)
                    {
                        creditReport.CreditAmount = creditAmount.CreditAmount;
                        creditReport.EscalatedLimit = creditAmount.EscalatedLimit;
                        creditReport.NonEscalatedLimit = creditAmount.NonEscalatedLimit;
                        creditReport.BeaconUpdated = creditReport.CreditLastUpdateTime > contract.LastUpdateTime;
                    }
                }
            }

            return creditReport;
        }

        private bool IsFrenchSymbols(Contract contract)
        {
            var result = IsCustomerHasFrenchSymbols(contract.PrimaryCustomer);
            foreach (var customer in contract.SecondaryCustomers.Where(sc => sc.IsDeleted != true))
            {
                if (IsCustomerHasFrenchSymbols(customer))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private bool IsCustomerHasFrenchSymbols(Customer customer)
        {
            var location = customer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress) ??
                                      customer.Locations?.FirstOrDefault();
           
            return customer.FirstName.IsFrenchSymbols() || customer.LastName.IsFrenchSymbols() ||
                   (location != null && (location.City.IsFrenchSymbols() || location.Street.IsFrenchSymbols()));
        }
    }


}
