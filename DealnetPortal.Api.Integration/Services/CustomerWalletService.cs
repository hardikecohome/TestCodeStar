using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.ServiceAgents;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.CustomerWallet;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Integration.Services
{
    public class CustomerWalletService : ICustomerWalletService
    {
        private readonly ICustomerWalletServiceAgent _customerWalletServiceAgent;
        private readonly IMailService _mailService;
        private readonly IContractRepository _contractRepository;
        private readonly ILoggingService _loggingService;

        public CustomerWalletService(
            ICustomerWalletServiceAgent customerWalletServiceAgent, 
            ILoggingService loggingService, 
            IMailService mailService, IContractRepository contractRepository)
        {
            _customerWalletServiceAgent = customerWalletServiceAgent;
            _loggingService = loggingService;
            _mailService = mailService;
            _contractRepository = contractRepository;
        }

        public async Task<IList<Alert>> CreateCustomerByContractList(List<Contract> contracts, string contractOwnerId)
        {
            var alerts = new List<Alert>();

            var customer = contracts.Select(x => x.PrimaryCustomer).FirstOrDefault();
            
            if (customer == null)
            {
                throw new NullReferenceException("contract.PrimaryCustomer");
            }

            var email = customer.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ?? customer.Emails.FirstOrDefault()?.EmailAddress;

            var randomPassword = RandomPassword.Generate();// System.Web.Security.Membership.GeneratePassword(12, 4);

            var transactionsInfoDtos = GetTransactionsInfoList(customer, contracts);

            //for transactionId and for send email select first contract with customer data
            var contract = contracts.First();

            var registerCustomer = new RegisterCustomerBindingModel
            {
                RegisterInfo = new RegisterBindingModel
                {
                    Email = email,
                    Password = randomPassword,
                    ConfirmPassword = randomPassword
                },
                Profile = new CustomerProfileDTO
                {
                    AccountId = customer.AccountId,
                    TransactionId = contract.Details?.TransactionId,
                    DateOfBirth = customer.DateOfBirth,
                    CreditAmount = contract.Details?.CreditAmount,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    EmailAddress = email,
                    Phones = AutoMapper.Mapper.Map<List<PhoneDTO>>(customer.Phones),
                    Locations = AutoMapper.Mapper.Map<List<LocationDTO>>(customer.Locations)
                },
                TransactionsInfo = transactionsInfoDtos
            };

            if (registerCustomer.Profile.Locations?.Any(l => l.AddressType == AddressType.InstallationAddress) == true)
            {
                registerCustomer.Profile.Locations.RemoveAll(l => l.AddressType != AddressType.InstallationAddress);
            }

            try
            {
                _loggingService.LogInfo(
                    $"Registration new {registerCustomer.RegisterInfo.Email} customer on CustomerWallet portal");
                var submitAlerts = await _customerWalletServiceAgent.RegisterCustomer(registerCustomer);
                if (submitAlerts?.Any() ?? false)
                {
                    alerts.AddRange(submitAlerts);
                }
                if (alerts.Any(a => a.Type == AlertType.Error))
                {

                    _loggingService.LogInfo(
                        $"Failed to register new {registerCustomer.RegisterInfo.Email} on CustomerWallet portal: {alerts.FirstOrDefault(a => a.Type == AlertType.Error)?.Header}");
                }
                //send email notification for DEAL-1490
                else
                {
                    await _mailService.SendInviteLinkToCustomer(contract, randomPassword);
                }
            }
            catch (HttpRequestException ex)
            {
                _loggingService.LogError("Cannot send request to CustomerWallet", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Cannot send request to CustomerWallet",
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public async Task<IList<Alert>> CheckCustomerExisting(string login)
        {
            var alerts = new List<Alert>();
            try
            {
                if (_contractRepository.IsMortgageBrokerCustomerExist(login))
                {
                    _loggingService.LogInfo($"Customer {login} already registered on Mortgage Broker Service.");
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "Cannot create customer",
                        Message = "Customer with this email address is already registered."
                    });
                }
                else
                {
                    var response = await _customerWalletServiceAgent.CheckUser(login);
                    if (response?.Any() ?? false)
                    {
                        _loggingService.LogInfo($"Customer {login} already registered on Customer Wallet.");
                        alerts.AddRange(response);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _loggingService.LogError("Cannot send request to CustomerWallet", ex);
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Cannot send request to CustomerWallet",
                    Message = ex.ToString()
                });
            }
            return alerts;
        }

        private List<TransactionInfoDTO> GetTransactionsInfoList(Customer customer, List<Contract> contracts)
        {
            return contracts.Select(contract => new TransactionInfoDTO
            {
                DealerName = contract.LastUpdateOperator,
                DealnetContractId = contract.Id,
                AspireAccountId = customer.AccountId,
                CreditAmount = contract.Details.CreditAmount,
                ScorecardPoints = contract.Details.ScorecardPoints,
                AspireStatus = contract.Details.Status,
                AspireTransactionId = contract.Details.TransactionId,
                EquipmentType = contract.Equipment?.NewEquipment?.FirstOrDefault()?.Type,
                IsIncomplete = contract.Equipment?.NewEquipment == null || !contract.Equipment.NewEquipment.Any() || customer.Locations.All(l => l.AddressType != AddressType.InstallationAddress),
                UpdateTime = DateTime.Now,
                CustomerComment = contract.Comments != null && contract.Comments.Any() ? contract.Comments.First().Text : null
            }).ToList();
        }
    }
}
