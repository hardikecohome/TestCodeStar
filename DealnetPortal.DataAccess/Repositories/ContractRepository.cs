using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Common.Types;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using Unity.Interception.Utilities;

namespace DealnetPortal.DataAccess.Repositories
{
    public class ContractRepository : BaseRepository, IContractRepository
    {
        private readonly IAppConfiguration _config;
        public ContractRepository(IDatabaseFactory databaseFactory, IAppConfiguration config) : base(databaseFactory)
        {
            _config = config;
        }

        #region Public
        public Contract CreateContract(string contractOwnerId)
        {
            Contract contract = null;
            var dealer = GetUserById(contractOwnerId);
            if (dealer != null)
            {
                contract = new Contract()
                {
                    ContractState = ContractState.Started,
                    CreationTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    Dealer = dealer,
                    CreateOperator = dealer.UserName,
                };
                _dbContext.Contracts.Add(contract);
            }
            return contract;
        }

        public IList<Contract> GetContracts(string ownerUserId)
        {
            var contracts = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.SecondaryCustomers.Select(sc => sc.EmploymentInfo))
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Documents)
                .Include(c => c.Signers)
                .Where(c => c.Dealer.Id == ownerUserId || c.Dealer.ParentDealerId == ownerUserId).ToList();
            return contracts;
        }

        public IList<Contract> GetDealerLeads(string userId)
        {
            var creditReviewStates = _config.GetSetting(WebConfigKeys.CREDIT_REVIEW_STATUS_CONFIG_KEY).Split(',').Select(s => s.Trim()).ToArray();
            var qcpclist = _config.GetSetting(WebConfigKeys.QUEBEC_POSTAL_CODES).Split(',').ToList();
            
            var contractCreatorRoleId = _dbContext.Roles.FirstOrDefault(r => r.Name == UserRole.CustomerCreator.ToString())?.Id;
            var dealerProfile = _dbContext.DealerProfiles.FirstOrDefault(p => p.DealerId == userId);
            var isQuebecDealer = dealerProfile?.Address.State == "QC";
            var eqList = dealerProfile?.Equipments.Select(e => e.Equipment.Type);
            var pcList = dealerProfile?.Areas.Select(e => e.PostalCode);
            if (eqList?.FirstOrDefault() == null || pcList?.FirstOrDefault() == null)
            {
                return new List<Contract>();
            }
            pcList = isQuebecDealer
                ? pcList?.Where(p => qcpclist.Any(qp => qp == p[0].ToString()))
                : pcList?.Where(p => qcpclist.All(qp => qp != p[0].ToString()));
            var contracts = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Documents)
                .Where(c => (c.IsCreatedByBroker == true
                || (contractCreatorRoleId == null || c.Dealer.Roles.Select(r => r.RoleId).Contains(contractCreatorRoleId))) &&
                c.Equipment.NewEquipment.Any() &&
                c.PrimaryCustomer.Locations.Any(l => l.AddressType == AddressType.InstallationAddress) &&
                (c.ContractState >= ContractState.CreditConfirmed && !creditReviewStates.Contains(c.Details.Status))).ToList();
            if (eqList!=null && eqList.Any())
            {
                contracts = contracts.Where(c => eqList.Any(eq => eq == c.Equipment?.NewEquipment?.FirstOrDefault()?.Type)).ToList();
            }
            if (pcList!=null && pcList.Any())
            {
                contracts = contracts.Where(c => pcList.Any(pc => c.PrimaryCustomer.Locations.FirstOrDefault(x => x.AddressType == AddressType.InstallationAddress).PostalCode.Length >= pc.Length &&
                c.PrimaryCustomer.Locations.FirstOrDefault(x => x.AddressType == AddressType.InstallationAddress).PostalCode.Substring(0, pc.Length) == pc)).ToList();
                //contracts = contracts.Where(c => pcList.Any(pc => 
                //    c.PrimaryCustomer.Locations?.FirstOrDefault(x => x.AddressType == AddressType.InstallationAddress)?.PostalCode.Contains(pc) ?? false)).ToList();
            }
             
            return contracts;
        }

        /// <summary>
        /// Get contract created by an user (dealer)
        /// </summary>
        /// <param name="ownerUserId">user Id</param>
        /// <returns>List of contracts</returns>
        public IList<Contract> GetContractsCreatedByUser(string userId)
        {
            var user = GetUserById(userId);
            var subdealer = user.SubDealers.AsEnumerable();

            var contracts = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Documents)
                .Where(c =>
                    c.CreateOperator == user.UserName && !string.IsNullOrEmpty(c.Details.TransactionId)).ToList();
            if (subdealer.Any())
            {
                foreach (var dealer in subdealer)
                {
                    contracts.AddRange(
                            _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Documents)
                .Where(c =>
                    c.CreateOperator == dealer.UserName && !string.IsNullOrEmpty(c.Details.TransactionId)).ToList()
                        );
                }
            }
            return contracts;
        }

        public int GetNewlyCreatedCustomersContractsCount(string ownerUserId)
        {
            var contractsCount = _dbContext.Contracts
                .Count(c => (c.Dealer.Id == ownerUserId || c.Dealer.ParentDealerId == ownerUserId) && c.IsNewlyCreated == true );
            return contractsCount;
        }

        public IList<Contract> GetContracts(IEnumerable<int> ids, string ownerUserId)
        {
            var contracts = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.PrimaryCustomer.EmploymentInfo)
                .Include(c => c.SecondaryCustomers)
                .Include(c=>c.SecondaryCustomers.Select(s=>s.EmploymentInfo))
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Equipment.InstallationPackages)
                .Include(c => c.Documents)
                .Include(c => c.Signers)
                .Where(
                    c =>
                        ids.Contains(c.Id) &&
                        (c.Dealer.Id == ownerUserId || c.Dealer.ParentDealerId == ownerUserId)).ToList();
            return contracts;
        }

        public IList<Contract> GetContractsEquipmentInfo(IEnumerable<int> ids, string ownerUserId)
        {
            var contracts = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Equipment.InstallationPackages)
                .Where(
                    c =>
                        ids.Contains(c.Id) &&
                        (c.Dealer.Id == ownerUserId || c.Dealer.ParentDealerId == ownerUserId)).ToList();           

            return contracts;
        }

        public bool IsMortgageBrokerCustomerExist(string email)
        {
            return _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Any(c => c.PrimaryCustomer.Emails.Any(e=>e.EmailAddress == email) &&
                    c.IsCreatedByBroker.HasValue && c.IsCreatedByBroker.Value);
        }

        public Contract FindContractBySignatureId(string signatureTransactionId)
        {
            return _dbContext.Contracts
                .Include(c => c.Signers)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.Equipment)
                .FirstOrDefault(c => c.Details.SignatureTransactionId == signatureTransactionId);
        }

        public ContractState? GetContractState(int contractId, string contractOwnerId)
        {
            return
                _dbContext.Contracts.AsNoTracking()
                    .FirstOrDefault(
                        c =>
                            c.Id == contractId &&
                            (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId))?
                    .ContractState;
        }

        public Contract UpdateContractState(int contractId, string contractOwnerId, ContractState newState)
        {
            var contract = GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                contract.ContractState = newState;

                if (contract.ContractState == ContractState.CreditCheckDeclined)
                {
                    AddOrUpdateInitialCustomers(contract);
                    contract.WasDeclined = true;
                }
                else
                {
                    contract.WasDeclined = false;
                }

                if (_dbContext.Entry(contract).State != EntityState.Unchanged)
                {
                    contract.LastUpdateTime = DateTime.UtcNow;
                    contract.LastUpdateOperator = GetDealer(contractOwnerId)?.UserName;
                }
            }
            return contract;
        }

        public Contract GetContract(int contractId, string contractOwnerId)
        {
            return _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.PrimaryCustomer.EmploymentInfo)
                .Include(c => c.PrimaryCustomer.CreditReport)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.SecondaryCustomers.Select(sc => sc.EmploymentInfo))
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.SalesRepInfo)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Equipment.InstallationPackages)
                .Include(c => c.Documents)
                .Include(c => c.Signers)
                .FirstOrDefault(
                    c =>
                        c.Id == contractId &&
                        (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId));
        }

        public Contract GetContract(int contractId)
        {
            return _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.PrimaryCustomer.EmploymentInfo)
                .Include(c => c.PrimaryCustomer.CreditReport)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Equipment.InstallationPackages)
                .Include(c => c.Documents)
                .Include(c => c.Signers)                
                .FirstOrDefault(
                    c =>
                        c.Id == contractId);
        }

        public Contract GetContractAsUntracked(int contractId, string contractOwnerId)
        {
            return _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.SecondaryCustomers)
                .Include(c => c.HomeOwners)
                .Include(c => c.InitialCustomers)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Include(c => c.Equipment.InstallationPackages)
                .Include(c => c.Signers)
                .Include(c => c.SalesRepInfo)
                .AsNoTracking().
                FirstOrDefault(
                    c =>
                        c.Id == contractId &&
                        (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId));
        }

        public Contract AssignContract(int contractId, string newContractOwnerId)
        {
            var updated = false;

            var contract = _dbContext.Contracts
               .Include(x => x.Equipment.NewEquipment)
               .Include(x => x.PrimaryCustomer)
               .Include(x => x.Dealer.Claims)
               .Include(c => c.PrimaryCustomer.Locations)
               .FirstOrDefault(x => x.Id == contractId);            

            //Change installation address to main address, and main to previous
            if (contract != null)
            {
                if (contract.IsCreatedByBroker == true || contract.IsCreatedByCustomer == true)
                {                    
                    if (contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress) != null)
                    {
                        var mainLoc = contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.MainAddress);
                        if (mainLoc != null)
                        {                            
                            var prevAddress = contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.PreviousAddress);
                            if (prevAddress != null)
                            {
                                _dbContext.Entry(prevAddress).State = EntityState.Deleted;
                            }
                            mainLoc.AddressType = AddressType.PreviousAddress;
                        }
                        //?
                        var installLoc = contract.PrimaryCustomer.Locations.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress);
                        if (installLoc != null)
                        {
                            installLoc.AddressType = AddressType.MainAddress;                            
                        }
                    }
                }

                var dealer = GetDealer(newContractOwnerId);

                if (dealer != null && (contract.IsCreatedByBroker == true || contract.IsCreatedByCustomer == true))
                {
                    contract.DealerId = dealer.Id;
                    contract.IsNewlyCreated = true;
                    contract.IsCreatedByBroker = false;
                    //contract.IsCreatedByCustomer = false;
                    contract.LastUpdateTime = DateTime.UtcNow;

                    _dbContext.Entry(contract).State = EntityState.Modified;

                    updated = true;
                }
            }

            return updated ? contract : null;
        }

        public bool DeleteContract(string contractOwnerId, int contractId)
        {
            bool deleted = false;
            var contract =
                _dbContext.Contracts.FirstOrDefault(
                    c =>
                        (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId) &&
                        c.Id == contractId);
            if (contract != null)
            {
                //remove clients for contract?
                contract.SecondaryCustomers.Clear();

                if (contract.Equipment != null)
                {
                    var entriesForDelete = contract.Equipment.NewEquipment.ToList();
                    entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

                    var entriesForDeleteE = contract.Equipment.ExistingEquipment.ToList();
                    entriesForDeleteE.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);
                }

                _dbContext.Contracts.Remove(contract);
                deleted = true;
            }
            return deleted;
        }

        public bool CleanContract(string contractOwnerId, int contractId)
        {
            bool cleaned = false;
            var contract =
                _dbContext.Contracts.FirstOrDefault(
                    c =>
                        (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId) &&
                        c.Id == contractId);
            if (contract != null)
            {
                contract.SecondaryCustomers.Clear();
                cleaned = true;
            }
            return cleaned;
        }

        public Contract UpdateContract(Contract contract, string contractOwnerId)
        {
            contract.ContractState = ContractState.CustomerInfoInputted;
            _dbContext.Entry(contract).State = EntityState.Modified;
            contract.LastUpdateTime = DateTime.UtcNow;
            contract.LastUpdateOperator = GetDealer(contractOwnerId)?.UserName;
            return contract;
        }

        public Contract UpdateContractData(ContractData contractData, string contractOwnerId)
        {
            if (contractData != null)
            {
                bool updated = false;
                var contract = GetContract(contractData.Id, contractOwnerId);
                if (contract != null)
                {
                    if (!string.IsNullOrEmpty(contractData.DealerId))
                    {
                        if (contract.DealerId != contractData.DealerId)
                        {
                            contract.DealerId = contractData.DealerId;
                            updated = true;
                        }
                    }
                    
                    if (contractData.ExternalSubDealerId != null &&
                        contract.ExternalSubDealerId != contractData.ExternalSubDealerId)
                    {
                        contract.ExternalSubDealerId = contractData.ExternalSubDealerId;
                        updated = true;
                    }
                    if (contractData.ExternalSubDealerName != null &&
                        contract.ExternalSubDealerName != contractData.ExternalSubDealerName)
                    {
                        contract.ExternalSubDealerName = contractData.ExternalSubDealerName;
                        updated = true;
                    }

                    if (contractData.PrimaryCustomer != null/* && contract.WasDeclined != true*/)
                    {
                        if (contractData.PrimaryCustomer.Id == 0 && contract.PrimaryCustomer != null)
                        {
                            contractData.PrimaryCustomer.Id = contract.PrimaryCustomer.Id;
                        }                        
                        var homeOwner = UpdateCustomer(contractData.PrimaryCustomer);
                        if (homeOwner != null)
                        {
                            contract.PrimaryCustomer = homeOwner;
                            var userUpdated = _dbContext.Entry(contract.PrimaryCustomer).State != EntityState.Unchanged
                                        || contract.PrimaryCustomer?.EmploymentInfo != null && _dbContext.Entry(contract.PrimaryCustomer?.EmploymentInfo).State != EntityState.Unchanged;
                            updated |= (userUpdated || _dbContext.Locations.Local.Any(l => _dbContext.Entry(l).State != EntityState.Unchanged)
                                        || _dbContext.Phones.Local.Any(p => _dbContext.Entry(p).State != EntityState.Unchanged)
                                        || _dbContext.Emails.Local.Any(e => _dbContext.Entry(e).State != EntityState.Unchanged)
                                        || _dbContext.ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted));
                            if (userUpdated && contract.ContractState != ContractState.Completed && contract.ContractState != ContractState.Closed)
                            {
                                contract.ContractState = ContractState.CustomerInfoInputted;
                            }
                        }
                    }

                    if (contractData.SecondaryCustomers != null)
                    {
                        var usersUpdated = AddOrUpdateAdditionalApplicants(contract, contractData.SecondaryCustomers);
                        usersUpdated |=
                            contract.SecondaryCustomers.Any(c => _dbContext.Entry(c).State != EntityState.Unchanged)
                            || _dbContext.Customers.Local.Any(c => _dbContext.Entry(c).State != EntityState.Unchanged);                        
                        updated |= (usersUpdated || _dbContext.Locations.Local.Any(l => _dbContext.Entry(l).State != EntityState.Unchanged)
                                    || _dbContext.Phones.Local.Any(p => _dbContext.Entry(p).State != EntityState.Unchanged)
                                    || _dbContext.Emails.Local.Any(e => _dbContext.Entry(e).State != EntityState.Unchanged)
                                    || _dbContext.ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted));
                        if (usersUpdated && contract.ContractState != ContractState.Completed && contract.ContractState != ContractState.Closed)
                        {
                            contract.ContractState = ContractState.CustomerInfoInputted;
                        }
                    }

                    if (contractData.HomeOwners != null)
                    {                        
                        var usersUpdated = AddOrUpdateHomeOwners(contract, contractData.HomeOwners);
                        if (usersUpdated && contract.ContractState != ContractState.Completed && contract.ContractState != ContractState.Closed)
                        {
                            contract.ContractState = ContractState.CustomerInfoInputted;
                        }
                        updated |= usersUpdated;
                    }

                    if (!contract.WasDeclined.HasValue || contract.WasDeclined == false)
                    {
                        updated |= AddOrUpdateInitialCustomers(contract);
                    }
                    
                    if (contractData.Equipment != null)
                    {                        
                        updated |= AddOrUpdateEquipment(contract, contractData.Equipment);
                    }                    

                    if (contractData.SalesRepInfo != null)
                    {
                        var salesRepRole = contractData.SalesRepInfo;
                        salesRepRole.Id = contract.Id;
                        salesRepRole.Contract = contract;
                        _dbContext.ContractSalesRepInfoes.AddOrUpdate(salesRepRole);                        
                        updated |= _dbContext.Entry(contract.SalesRepInfo).State != EntityState.Unchanged;
                    }

                    if (contractData.Details != null)
                    {
                        AddOrUpdateContactDetails(contract, contractData.Details);
                        updated |= _dbContext.Entry(contract).State != EntityState.Unchanged;
                    }

                    //this condition after equipment and details updates
                    if (contract.Equipment != null)
                    {
                        var paymentSummary = GetContractPaymentsSummary(contract);
                        contract.Equipment.ValueOfDeal = contract.Equipment.AgreementType == AgreementType.LoanApplication ? paymentSummary.TotalAmountFinanced : paymentSummary.TotalMonthlyPayment;

                        if (!IsBill59Contract(contract.Id))
                        {
                            contract.Equipment.HasExistingAgreements = null;
                        }
                    }

                    if (contractData.PaymentInfo != null)
                    {
                        AddOrUpdatePaymentInfo(contract, contractData.PaymentInfo);
                        updated |= _dbContext.Entry(contract.PaymentInfo).State != EntityState.Unchanged;
                    }

                    updated |= _dbContext.Entry(contract).State != EntityState.Unchanged;

                    if (updated)
                    {
                        contract.LastUpdateTime = DateTime.UtcNow;
                        contract.LastUpdateOperator = GetDealer(contractOwnerId)?.UserName;
                    }

                    return contract;
                }
            }
            return null;
        }

        public Customer UpdateCustomer(Customer customer)
        {
            if (customer != null)
            {
                var locations = customer.Locations;
                var emails = customer.Emails;
                var phones = customer.Phones;                

                var dbCustomer = AddOrUpdateCustomer(customer);
                if (dbCustomer != null)
                {
                    if (locations != null)
                    {
                        AddOrUpdateCustomerLocations(dbCustomer, locations.ToList());
                    }
                    if (phones != null)
                    {
                        AddOrUpdateCustomerPhones(dbCustomer, phones.ToList());
                    }
                    if (emails != null)
                    {
                        AddOrUpdateCustomerEmails(dbCustomer, emails.ToList());
                    }
                }
                return dbCustomer;
            }
            return null;
        }

        public bool UpdateCustomerData(int customerId, Customer customerInfo, IList<Location> locations,
            IList<Phone> phones, IList<Email> emails)
        {
            bool updated = false;
            var dbCustomer = GetCustomer(customerId);
            if (dbCustomer != null)
            {
                if (customerInfo != null)
                {
                    if (customerInfo.AllowCommunicate.HasValue)
                    {
                        dbCustomer.AllowCommunicate = customerInfo.AllowCommunicate;
                    }
                    if (customerInfo.PreferredContactMethod.HasValue)
                    {
                        dbCustomer.PreferredContactMethod = customerInfo.PreferredContactMethod;
                    }
                    if (!string.IsNullOrWhiteSpace(customerInfo.DriverLicenseNumber))
                    {
                        dbCustomer.DriverLicenseNumber = customerInfo.DriverLicenseNumber;
                    }
                    if (!string.IsNullOrWhiteSpace(customerInfo.Sin))
                    {
                        dbCustomer.Sin = customerInfo.Sin;
                    }
                    if (customerInfo.AccountId != null)
                    {
                        dbCustomer.AccountId = customerInfo.AccountId;
                    }
                    if (!string.IsNullOrWhiteSpace(customerInfo.DealerInitial))
                    {
                        dbCustomer.DealerInitial = customerInfo.DealerInitial;
                    }
                    if (!string.IsNullOrWhiteSpace(customerInfo.VerificationIdName))
                    {
                        dbCustomer.VerificationIdName = customerInfo.VerificationIdName;
                    }

                    if (customerInfo.EmploymentInfo != null)
                    {
                        AddOrUpdateEmploymentInfo(dbCustomer, customerInfo.EmploymentInfo);
                    }

                    if (customerInfo.CreditReport != null)
                    {
                        AddOrUpdateCustomerCreditReport(dbCustomer, customerInfo.CreditReport);
                    }
                }

                updated = _dbContext.Entry(dbCustomer).State != EntityState.Unchanged;

                if (locations != null)
                {
                    updated |= AddOrUpdateCustomerLocations(dbCustomer, locations.ToList());
                }
                if (phones != null)
                {
                    updated |= AddOrUpdateCustomerPhones(dbCustomer, phones.ToList());
                }
                if (emails != null)
                {
                    updated |= AddOrUpdateCustomerEmails(dbCustomer, emails.ToList());
                }
                //return dbCustomer;
            }
            return updated;
        }

        public bool UpdateCustomerEmails(int customerId, IList<Email> emails)
        {
            bool updated = false;
            var dbCustomer = GetCustomer(customerId);
            if (dbCustomer != null)
            {
                emails?.ForEach(email =>
                {
                    var curEmail =
                        dbCustomer.Emails.FirstOrDefault(e => e.EmailType == email.EmailType);
                    if (curEmail == null)
                    {
                        email.Customer = dbCustomer;
                        dbCustomer.Emails.Add(email);
                        updated = true;
                    }
                    else
                    {
                        curEmail.EmailAddress = email.EmailAddress;
                        updated |= _dbContext.Entry(curEmail).State != EntityState.Unchanged;
                    }
                });
            }
            return updated;
        }

        public Customer GetCustomer(int customerId)
        {
            return _dbContext.Customers
                .Include(c => c.Phones)
                .Include(c => c.Locations)
                .Include(c => c.Emails)
                .Include(c => c.EmploymentInfo)
                .FirstOrDefault(c => c.Id == customerId);
        }

        public IList<EquipmentType> GetEquipmentTypes()
        {
            return _dbContext.EquipmentTypes.ToList();
        }

        public EquipmentType GetEquipmentTypeInfo(string type)
        {
            return _dbContext.EquipmentTypes.FirstOrDefault(et => et.Type == type);
        }

        public IList<DocumentType> GetStateDocumentTypes(string state)
        {
            var checkList = _dbContext.FundingCheckLists
                .Include(s => s.FundingDocumentList)
                .Include(s => s.FundingDocumentList.FundingCheckDocuments)
                .FirstOrDefault(l => l.Province == state && string.IsNullOrEmpty(l.DealerId))?.
                FundingDocumentList?.FundingCheckDocuments.Select(d => d.DocumentType).ToList();
            return checkList;
        }

        public IList<DocumentType> GetDealerDocumentTypes(string state, string contractOwnerId)
        {
            var checkList = _dbContext.FundingCheckLists
                    .Include(s => s.FundingDocumentList)
                    .Include(s => s.FundingDocumentList.FundingCheckDocuments)
                .FirstOrDefault(l => l.DealerId == contractOwnerId && l.Province == state);
            if (checkList == null)
            {
                checkList = _dbContext.FundingCheckLists
                    .Include(s => s.FundingDocumentList)
                    .Include(s => s.FundingDocumentList.FundingCheckDocuments)
                    .FirstOrDefault(l => l.Province == state);
            }
            if (checkList != null)
            {
                return checkList.FundingDocumentList.FundingCheckDocuments.Select(d => d.DocumentType).ToList();
            }
            return new List<DocumentType>();                
        }

        public IDictionary<string, IList<DocumentType>> GetDealerDocumentTypes(string contractOwnerId)
        {
            var lists = _dbContext.FundingCheckLists
                .Include(s => s.FundingDocumentList)
                .Include(s => s.FundingDocumentList.FundingCheckDocuments)
                .Where(l => l.DealerId == contractOwnerId || string.IsNullOrEmpty(l.DealerId)).ToList();
            var provLists = lists.GroupBy(l => l.Province)
                .ToDictionary(g => g.Key, g => (IList<DocumentType>)(g.FirstOrDefault(fl => !string.IsNullOrEmpty(fl.DealerId)) ?? g.FirstOrDefault())?
                                                                .FundingDocumentList?.FundingCheckDocuments?.Select(fcd => fcd.DocumentType).ToList());
            return provLists;
        }

        public IList<DocumentType> GetAllDocumentTypes()
        {
            return _dbContext.DocumentTypes.ToList();
        }

        /// Get Document Types specified for contract
        public IList<DocumentType> GetContractDocumentTypes(int contractId, string contractOwnerId)
        {
            var contract = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .FirstOrDefault(c => c.Id == contractId &&
                                     (c.Dealer.Id == contractOwnerId || c.Dealer.ParentDealerId == contractOwnerId));
            if (contract != null)
            {
                var state = contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress)?.State;
                state = state ?? contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress)?.State;
                return GetDealerDocumentTypes(state, contractOwnerId);
            }
            return new List<DocumentType>();
        }

        public ProvinceTaxRate GetProvinceTaxRate(string province)
        {
            return _dbContext.ProvinceTaxRates.FirstOrDefault(x => x.Province == province);
        }

        public IList<ProvinceTaxRate> GetAllProvinceTaxRates()
        {
            return _dbContext.ProvinceTaxRates.ToList();
        }

        public VerifiactionId GetVerficationId(int id)
        {
            return _dbContext.VerificationIds.FirstOrDefault(x => x.Id == id);
        }

        public IList<VerifiactionId> GetAllVerificationIds()
        {
            return _dbContext.VerificationIds.ToList();
        }

        public AspireStatus GetAspireStatus(string status)
        {
            return _dbContext.AspireStatuses.FirstOrDefault(x => x.Status.Contains(status.Trim()));
        }  

        public PaymentSummary GetContractPaymentsSummary(int contractId, decimal? priceOfEquipment)
        {
            PaymentSummary paymentSummary = null;

            var contract = GetContract(contractId);
            if (contract != null)
            {
                paymentSummary = GetContractPaymentsSummary(contract, priceOfEquipment);                
            }

            return paymentSummary;
        }

        public ContractDocument AddDocumentToContract(int contractId, ContractDocument document, string contractOwnerId)
        {
            var contract = _dbContext.Contracts.Include(c => c.Documents).FirstOrDefault(c => c.Id == contractId);
            var docTypeId = document.DocumentTypeId;
            if (document.DocumentType != null)
            {
                docTypeId =
                    _dbContext.DocumentTypes.FirstOrDefault(t => t.Prefix == document.DocumentType.Prefix)?.Id ??
                    docTypeId;
            }
            var docType = _dbContext.DocumentTypes.Find(docTypeId);
            if (!string.IsNullOrEmpty(docType?.Prefix))
            {
                if (string.IsNullOrEmpty(document.DocumentName) || !document.DocumentName.StartsWith(docType.Prefix))
                {
                    document.DocumentName = docType.Prefix + document.DocumentName;
                }
            }

            document.CreationDate = DateTime.UtcNow;

            if (contract != null)
            {
                var otherType = _dbContext.DocumentTypes.FirstOrDefault(t => string.IsNullOrEmpty(t.Prefix));
                var dbDocument =
                    contract.Documents.FirstOrDefault(
                        d =>
                            otherType != null && otherType.Id == docTypeId
                                ? d.DocumentTypeId == docTypeId && d.Id == document.Id
                                : d.DocumentTypeId == docTypeId);
                if (dbDocument != null)
                {
                    //var otherType = _dbContext.DocumentTypes.FirstOrDefault(t => string.IsNullOrEmpty(t.Prefix));
                    //if (otherType != null && dbDocument.DocumentType.Id == otherType.Id && dbDocument.DocumentName != document.DocumentName)
                    //{
                    //    document.Contract = contract;
                    //    contract.Documents.Add(document);
                    //}
                    //else
                    //{
                    document.Id = dbDocument.Id;
                    document.Contract = contract;
                    _dbContext.ContractDocuments.AddOrUpdate(document);
                    //}                    
                }
                else
                {
                    document.Contract = contract;
                    contract.Documents.Add(document);
                }
            }
            return document;
        }

        public bool TryRemoveContractDocument(int documentId, string contractOwnerId)
        {
            var document = _dbContext.ContractDocuments.FirstOrDefault(x => x.Id == documentId);
            if (document == null)
            {
                return false;
            }
            if (!CheckContractAccess(document.ContractId, contractOwnerId))
            {
                return false;
            }
            _dbContext.ContractDocuments.Remove(document);
            return true;
        }

        public IList<ContractDocument> GetContractDocumentsList(int contractId, string contractOwnerId)
        {
            var contract = _dbContext.Contracts.Find(contractId);
            return contract.Documents.ToList();
        }

        public IList<ApplicationUser> GetSubDealers(string dealerId)
        {
            var dealer = _dbContext.Users.Find(dealerId);
            return dealer?.SubDealers.ToList() ?? new List<ApplicationUser>();
        }

        public ApplicationUser GetDealer(string dealerId)
        {
            return _dbContext.Users.Find(dealerId);
        }

        public int UpdateSubDealersHierarchyByRelatedTransactions(IEnumerable<string> transactionIds, string ownerUserId)
        {
            int updated = 0;

            var dbTransactions =
                _dbContext.Contracts.Where(c => !string.IsNullOrEmpty(c.Details.TransactionId))
                    .Select(t => t.Details.TransactionId)
                    .ToList();
            dbTransactions = dbTransactions.Intersect(transactionIds).ToList();

            var dealers =
                _dbContext.Contracts.Where(
                    c =>
                        dbTransactions.Any(t => t == c.Details.TransactionId) &&
                        (c.Dealer.Id != ownerUserId && c.Dealer.ParentDealerId != ownerUserId))
                    .Select(c => c.Dealer).ToArray();
            if (dealers?.Any() ?? false)
            {
                updated = dealers.Length;
                dealers.ForEach(d => d.ParentDealerId = ownerUserId);
            }

            return updated;
        }        

        public ContractData GetContractData(int contractId, string contractOwnerId)
        {
            ContractData contractData = new ContractData()
            {
                Id = contractId
            };
            var contract = GetContractAsUntracked(contractId, contractOwnerId);
            if (contract != null)
            {
                contractData.SecondaryCustomers = contract.SecondaryCustomers?.ToList();
                contractData.Equipment = contract.Equipment;
            }
            return contractData;
        }

        public Comment TryAddComment(Comment comment, string contractOwnerId)
        {
            //if (!CheckContractAccess(comment.ContractId, contractOwnerId)) { return false; }
            var dealer = GetUserById(contractOwnerId);
            comment.Date = DateTime.UtcNow;
            comment.Dealer = dealer;
            _dbContext.Comments.AddOrUpdate(comment);
            return comment;
        }

        public int? RemoveComment(int commentId, string contractOwnerId)
        {
            var cmmnt = _dbContext.Comments.FirstOrDefault(x => x.Id == commentId && x.DealerId == contractOwnerId);
            if (cmmnt == null || cmmnt.Replies.Any())
            {
                return null;
            }
            var cmmntId = cmmnt.ContractId;
            _dbContext.Comments.Remove(cmmnt);
            return cmmntId;
        }

        public bool IsContractUnassignable(int contractId)
        {
            var creditReviewStates = _config.GetSetting(WebConfigKeys.CREDIT_REVIEW_STATUS_CONFIG_KEY).Split(',').Select(s => s.Trim()).ToArray();

            var contract = _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.Dealer.Roles)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.NewEquipment)
                .SingleOrDefault(c => c.Id == contractId);
            if (contract == null)
            {
                return false;
            }
            if (contract.ContractState < ContractState.CreditConfirmed || creditReviewStates.Contains(contract.Details.Status))
            {
                return false;
            }
            var contractEquipment = contract.Equipment?.NewEquipment.Select(e => e.Type).FirstOrDefault();
            var contractPostalCode = contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress)?.PostalCode;
            return !_dbContext.DealerProfiles.Any(dp => dp.Equipments.Any(e => e.Equipment.Type == contractEquipment)
                                                        && dp.Areas.Any(a => a.PostalCode.Length <= contractPostalCode.Length &&
                                                            contractPostalCode.Substring(0, a.PostalCode.Length) == a.PostalCode));
        }

        public bool IsClarityProgram(int contractId)
        {
            bool isClarity = false;
            var contract = _dbContext.Contracts
                .Include(c => c.Equipment)
                //.Include(c => c.Equipment.RateCard)
                //.Include(c => c.Equipment.RateCard.Tier)
                .FirstOrDefault(c => c.Id == contractId);
            if (contract != null)
            {
                var rateCard = contract.Equipment?.RateCardId != null ? _dbContext.RateCards
                    .Include(r => r.Tier)
                    .FirstOrDefault(r => r.Id == contract.Equipment.RateCardId) : null;
                isClarity = rateCard?.Tier?.Name == _config.GetSetting(WebConfigKeys.CLARITY_TIER_NAME) && contract.Equipment?.IsClarityProgram == true;
            }
            return isClarity;
        }

        public bool IsBill59Contract(int contractId)
        {
            bool isBill59 = false;

            var contract = _dbContext.Contracts
                .Include(c => c.Equipment)
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.Equipment.NewEquipment)
                .FirstOrDefault(c => c.Id == contractId);


            if (contract != null)
            {
                var location = contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.MainAddress) ??
                           contract.PrimaryCustomer?.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress) ?? contract.PrimaryCustomer?.Locations?.FirstOrDefault();
                if (contract.Details.AgreementType != AgreementType.LoanApplication &&
                    location?.State?.ToProvinceCode() == "ON")
                {
                    isBill59 = contract.Equipment?.NewEquipment?.Any(ne => GetEquipmentTypeInfo(ne.Type)?.UnderBill59 ?? false) ?? false;
                }                    
            }

            return isBill59;
        }

        public IList<Contract> GetExpiredContracts(DateTime expiredDate)
        {
            var contractCreatorRoleId = _dbContext.Roles.FirstOrDefault(r => r.Name == UserRole.CustomerCreator.ToString())?.Id;

            return _dbContext.Contracts
                .Include(c => c.PrimaryCustomer)
                .Include(c => c.Dealer.Roles)
                .Include(c => c.PrimaryCustomer.Locations)
                .Include(c => c.Equipment)
                .Include(c => c.Equipment.ExistingEquipment)
                .Include(c => c.Equipment.NewEquipment)
                .Where(c => c.CreationTime <= expiredDate && 
                (c.IsCreatedByBroker==true || c.Dealer.Roles.Select(r => r.RoleId).Contains(contractCreatorRoleId)) &&
                c.PrimaryCustomer.Locations.Any(l=>l.AddressType == AddressType.InstallationAddress) &&
                c.Equipment.NewEquipment.Any()).ToList();
        }

        public Contract UpdateContractAspireSubmittedDate(int contractId, string contractOwnerId)
        {
            var contract = GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                if (!contract.DateOfSubmit.HasValue)
                {
                    contract.DateOfSubmit = DateTime.UtcNow;
                }
                contract.LastUpdateTime = DateTime.UtcNow;
                contract.LastUpdateOperator = GetDealer(contractOwnerId)?.UserName;
            }
            return contract;
        }

        public Contract UpdateContractSigners(int contractId, IList<ContractSigner> signers, string contractOwnerId, bool syncOnly = false)
        {
            var contract = GetContract(contractId, contractOwnerId);
            if (contract != null)
            {
                if (!syncOnly)
                {
                    var existingEntities =
                        contract.Signers.Where(
                            cs => signers.Any(s => s.Id != 0 && s.Id == cs.Id
                                                   || cs.CustomerId != null && s.CustomerId == cs.CustomerId
                                                   || cs.SignerType == s.SignerType)).ToList();
                    var entriesForDelete = contract.Signers.Except(existingEntities).ToList();
                    entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);
                }

                signers.ForEach(s =>
                {
                    var curSigner =
                        contract.Signers.FirstOrDefault(cs => s.Id != 0 && cs.Id == s.Id 
                            || (s.CustomerId != null && cs.CustomerId == s.CustomerId)
                            || cs.SignerType == s.SignerType);
                    if (curSigner == null)
                    {
                        if (!syncOnly)
                        {
                            s.Contract = contract;
                            s.ContractId = contractId;
                            contract.Signers.Add(s);
                        }
                    }
                    else
                    {
                        if (curSigner.FirstName != s.FirstName)
                        {
                            curSigner.FirstName = s.FirstName;
                        }
                        if (curSigner.LastName != s.LastName)
                        {
                            curSigner.LastName = s.LastName;
                        }
                        if (curSigner.CustomerId != s.CustomerId)
                        {
                            curSigner.CustomerId = s.CustomerId;
                        }
                        if (!string.IsNullOrEmpty(s.EmailAddress) && curSigner.EmailAddress != s.EmailAddress)
                        {
                            curSigner.EmailAddress = s.EmailAddress;
                        }
                        if (curSigner.SignerType != s.SignerType)
                        {
                            curSigner.SignerType = s.SignerType;
                        }
                        if (!string.IsNullOrEmpty(s.Comment) && curSigner.Comment != s.Comment)
                        {
                            curSigner.Comment = s.Comment;
                        }
                    }
                });
            }
            return contract;
        }
        #endregion

        #region Private
        private bool CheckContractAccess(int contractId, string contractOwnerId)
        {
            return _dbContext.Contracts
                .Any(c => c.Id == contractId && c.Dealer.Id == contractOwnerId);
        }

        private bool AddOrUpdateEquipment(Contract contract, EquipmentInfo equipmentInfo)
        {
            var updated = false;
            var newEquipments = equipmentInfo.NewEquipment;
            var existingEquipments = equipmentInfo.ExistingEquipment;
            var installationPackages = equipmentInfo.InstallationPackages;
            var dbEquipment = _dbContext.EquipmentInfo.Find(equipmentInfo.Id);

            if (dbEquipment == null || dbEquipment.Id == 0)
            {
                equipmentInfo.ExistingEquipment = new List<ExistingEquipment>();
                equipmentInfo.NewEquipment = new List<NewEquipment>();
                equipmentInfo.InstallationPackages = new List<InstallationPackage>();
                equipmentInfo.Contract = contract;
                dbEquipment = _dbContext.EquipmentInfo.Add(equipmentInfo);
            }
            else
            {
                UpdateEquipmentBaseInfo(dbEquipment, equipmentInfo);               
            }

            updated = _dbContext.Entry(dbEquipment).State != EntityState.Unchanged;

            if (newEquipments != null)
            {
                updated |= AddOrUpdateNewEquipments(dbEquipment, newEquipments);
            }

            if (existingEquipments != null)
            {
                updated |= AddOrUpdateExistingEquipments(dbEquipment, existingEquipments);
            }

            if (installationPackages != null)
            {
                updated |= AddOrUpdateInstallationPackages(dbEquipment, installationPackages);
            }            
            
            updated |= _dbContext.Entry(dbEquipment).State != EntityState.Unchanged;

            return updated;
        }

        private PaymentSummary GetContractPaymentsSummary(Contract contract, decimal? priceOfEquipment = null)
        {
            PaymentSummary paymentSummary = new PaymentSummary();

            if (contract != null)
            {
                var rate =
                    GetProvinceTaxRate(
                        (contract.PrimaryCustomer?.Locations.FirstOrDefault(
                             l => l.AddressType == AddressType.MainAddress) ??
                         contract.PrimaryCustomer?.Locations.First())?.State.ToProvinceCode());

                if (contract.Equipment.AgreementType == AgreementType.LoanApplication)
                {
                    //for loan
                    if (!priceOfEquipment.HasValue)
                    {
                        priceOfEquipment = 0.0m;
                        if (IsClarityProgram(contract.Id))
                        {
                            //for Clarity program we have different logic
                            priceOfEquipment =
                                (contract.Equipment?.NewEquipment?.Where(ne => ne.IsDeleted != true)
                                              .Sum(x => x.MonthlyCost) ?? 0.0m);
                            var packages =
                                (contract.Equipment?.InstallationPackages?.Sum(x => x.MonthlyCost) ?? 0.0m);
                            priceOfEquipment += packages;
                        }
                        else
                        {
                            priceOfEquipment = contract.Equipment?.NewEquipment
                                                   ?.Where(ne => ne.IsDeleted != true)
                                                   .Sum(x => x.Cost) ?? 0;
                        }
                    }

                    if (contract.Equipment?.AmortizationTerm != null && contract.Equipment?.LoanTerm != null)
                    {
                        var loanCalculatorInput = new LoanCalculator.Input
                        {
                            TaxRate = rate?.Rate ?? 0,
                            LoanTerm = contract.Equipment?.LoanTerm ?? 0,
                            AmortizationTerm = contract.Equipment?.AmortizationTerm ?? 0,
                            PriceOfEquipment = (double?)priceOfEquipment ?? 0.0,
                            AdminFee = contract.Equipment?.IsFeePaidByCutomer == true ? (double)(contract.Equipment?.AdminFee ?? 0) : 0.0,
                            DownPayment = (double)(contract.Equipment?.DownPayment ?? 0),
                            CustomerRate = (double)(contract.Equipment?.CustomerRate ?? 0),
                            IsClarity = contract.Dealer?.Tier?.Name ==
                                        _config.GetSetting(WebConfigKeys.CLARITY_TIER_NAME),
                            IsOldClarityDeal = contract.Equipment?.IsClarityProgram == null &&
                                               contract.Dealer?.Tier?.Name ==
                                               _config.GetSetting(WebConfigKeys.CLARITY_TIER_NAME)
                        };
                        var loanCalculatorOutput = LoanCalculator.Calculate(loanCalculatorInput);
                        paymentSummary.LoanDetails = loanCalculatorOutput;

                        paymentSummary.Hst = (decimal) loanCalculatorOutput.Hst;
                        paymentSummary.TotalMonthlyPayment = (decimal) loanCalculatorOutput.TotalMonthlyPayment;
                        paymentSummary.MonthlyPayment = (decimal) loanCalculatorOutput.TotalMonthlyPayment;
                        paymentSummary.TotalAllMonthlyPayment = (decimal) loanCalculatorOutput.TotalAllMonthlyPayments;
                        paymentSummary.TotalAmountFinanced = (decimal) loanCalculatorOutput.TotalAmountFinanced;
                    }
                }
                else
                {
                    //for rental!
                    paymentSummary.MonthlyPayment = contract.Equipment?.TotalMonthlyPayment;
                    if (contract.Equipment?.TotalMonthlyPayment != null && contract.Equipment?.RequestedTerm != null)
                    {
                        paymentSummary.Hst =
                            (contract.Equipment?.TotalMonthlyPayment ?? 0) * (((decimal?) rate?.Rate ?? 0.0m) / 100);
                        paymentSummary.TotalMonthlyPayment = (contract.Equipment.TotalMonthlyPayment ?? 0) +
                                                             (contract.Equipment.TotalMonthlyPayment ?? 0) *
                                                             (((decimal?) rate?.Rate ?? 0.0m) / 100);
                        paymentSummary.TotalAllMonthlyPayment = paymentSummary.TotalMonthlyPayment *
                                                                (contract.Equipment.RequestedTerm ?? 0);
                        paymentSummary.TotalAmountFinanced = paymentSummary.TotalAllMonthlyPayment;
                        paymentSummary.SoftCapLimit =
                        contract.Equipment.NewEquipment.Any(eq => eq.MonthlyCost > eq.EquipmentType?.SoftCap && eq.EquipmentType?.HardCap >= eq.MonthlyCost) ||
                        contract.Equipment.NewEquipment.Any(eq => eq.MonthlyCost > eq.EquipmentSubType?.SoftCap  && eq.EquipmentSubType?.HardCap >= eq.MonthlyCost);
                    }
                }
            }

            return paymentSummary;
        }

        private EquipmentInfo UpdateEquipmentBaseInfo(EquipmentInfo dbEquipment, EquipmentInfo equipmentInfo)
        {            
            if (!string.IsNullOrEmpty(equipmentInfo.SalesRep))
            {
                dbEquipment.SalesRep = equipmentInfo.SalesRep;
            }
            if (equipmentInfo.DownPayment != null)
            {
                dbEquipment.DownPayment = equipmentInfo.DownPayment;
            }
            if (equipmentInfo.CustomerRate != null)
            {
                dbEquipment.CustomerRate = equipmentInfo.CustomerRate;
            }
            if (equipmentInfo.TotalMonthlyPayment != null)
            {
                dbEquipment.TotalMonthlyPayment = equipmentInfo.TotalMonthlyPayment;
            }
            if (equipmentInfo.RequestedTerm != null)
            {
                dbEquipment.RequestedTerm = equipmentInfo.RequestedTerm;
            }
            if (equipmentInfo.LoanTerm != null)
            {
                dbEquipment.LoanTerm = equipmentInfo.LoanTerm;
            }
            if (equipmentInfo.AmortizationTerm != null)
            {
                dbEquipment.AmortizationTerm = equipmentInfo.AmortizationTerm;
            }
            if (equipmentInfo.DeferralType != null)
            {
                dbEquipment.DeferralType = equipmentInfo.DeferralType;
            }
            if (equipmentInfo.AdminFee != null)
            {
                dbEquipment.AdminFee = equipmentInfo.AdminFee;
            }
            if (equipmentInfo.ValueOfDeal != null)
            {
                dbEquipment.ValueOfDeal = equipmentInfo.ValueOfDeal;
            }
            if (!string.IsNullOrEmpty(equipmentInfo.InstallerFirstName))
            {
                dbEquipment.InstallerFirstName = equipmentInfo.InstallerFirstName;
            }
            if (!string.IsNullOrEmpty(equipmentInfo.InstallerLastName))
            {
                dbEquipment.InstallerLastName = equipmentInfo.InstallerLastName;
            }
            if (equipmentInfo.InstallationDate.HasValue)
            {
                dbEquipment.InstallationDate = equipmentInfo.InstallationDate;
            }
            if (equipmentInfo.EstimatedInstallationDate.HasValue)
            {
                dbEquipment.EstimatedInstallationDate = equipmentInfo.EstimatedInstallationDate;
            }
            if (equipmentInfo.CustomerRiskGroupId.HasValue)
            {
                dbEquipment.CustomerRiskGroupId = equipmentInfo.CustomerRiskGroupId;
            }
            if (equipmentInfo.IsFeePaidByCutomer.HasValue)
            {
                dbEquipment.IsFeePaidByCutomer = equipmentInfo.IsFeePaidByCutomer;
            }
            if (equipmentInfo.DealerCost.HasValue)
            {
                dbEquipment.DealerCost = equipmentInfo.DealerCost;
            }
            if (equipmentInfo.PreferredStartDate.HasValue)
            {
                dbEquipment.PreferredStartDate = equipmentInfo.PreferredStartDate;
            }
            if (equipmentInfo.IsClarityProgram.HasValue)
            {
                dbEquipment.IsClarityProgram = equipmentInfo.IsClarityProgram;
            }
            if (equipmentInfo.HasExistingAgreements.HasValue)
            {
                dbEquipment.HasExistingAgreements = equipmentInfo.HasExistingAgreements;
            }
            if (equipmentInfo.RentalProgramType.HasValue)
            {
                dbEquipment.RentalProgramType = equipmentInfo.RentalProgramType;
            }
            dbEquipment.RateCardId = equipmentInfo.RateCardId;
            dbEquipment.RateReduction = equipmentInfo.RateReduction;
            dbEquipment.RateReductionCost = equipmentInfo.RateReductionCost;
		    dbEquipment.RateReductionCardId = equipmentInfo.RateReductionCardId;

            return dbEquipment;
        }

        private bool AddOrUpdateNewEquipments(EquipmentInfo dbEquipment, IEnumerable<NewEquipment> newEquipments)
        {
            var updated = false;
            var existingEntities =
                dbEquipment.NewEquipment.Where(
                    a => newEquipments.Any(ne => ne.Id == a.Id) || a.IsDeleted == true).ToList();
            var entriesForDelete = dbEquipment.NewEquipment.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e =>
            {
                e.IsDeleted = true;
                e.Cost = 0.0m;
                e.MonthlyCost = 0.0m;
            });

            newEquipments.ForEach(ne =>
            {
                var curEquipment =
                    dbEquipment.NewEquipment.FirstOrDefault(eq => eq.Id == ne.Id);
                if (curEquipment == null || ne.Id == 0)
                {
                    dbEquipment.NewEquipment.Add(ne);
                    updated = true;
                }
                else
                {                    
                    if (!string.IsNullOrEmpty(ne.InstalledModel))
                    {
                        curEquipment.InstalledModel = ne.InstalledModel;
                    }
                    if (!string.IsNullOrEmpty(ne.InstalledSerialNumber))
                    {
                        curEquipment.InstalledSerialNumber = ne.InstalledSerialNumber;
                    }
                    if (ne.EstimatedInstallationDate.HasValue)
                    {
                        curEquipment.EstimatedInstallationDate = ne.EstimatedInstallationDate;
                    }
                    if (ne.EstimatedInstallationDate.HasValue)
                    {
                        curEquipment.EstimatedInstallationDate = ne.EstimatedInstallationDate;
                    }
                    if (!string.IsNullOrEmpty(ne.Type))
                    {
                        curEquipment.Type = ne.Type;
                    }
                    if (!string.IsNullOrEmpty(ne.Description))
                    {
                        curEquipment.Description = ne.Description;
                    }
                    if (ne.Cost.HasValue)
                    {
                        curEquipment.Cost = ne.Cost;
                    }
                    if (ne.MonthlyCost.HasValue)
                    {
                        curEquipment.MonthlyCost = ne.MonthlyCost;
                    }
                    if (ne.EstimatedRetailCost.HasValue)
                    {
                        curEquipment.EstimatedRetailCost = ne.EstimatedRetailCost;
                    }
                    curEquipment.EquipmentSubTypeId = ne.EquipmentSubTypeId > 0 ? ne.EquipmentSubTypeId : null;                    
                    curEquipment.EquipmentTypeId = ne.EquipmentTypeId > 0 ? ne.EquipmentTypeId : null;                    
                    updated |= _dbContext.Entry(curEquipment).State != EntityState.Unchanged;
                }
            });
            return updated;
        }

        private bool AddOrUpdateExistingEquipments(EquipmentInfo dbEquipment, IEnumerable<ExistingEquipment> existingEquipments)
        {
            var updated = false;
            var existingEntities =
                dbEquipment.ExistingEquipment.Where(
                    a => existingEquipments.Any(ee => ee.Id == a.Id)).ToList();
            var entriesForDelete = dbEquipment.ExistingEquipment.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => _dbContext.ExistingEquipment.Remove(e));

            existingEquipments.ForEach(ee =>
            {
                var curEquipment =
                    dbEquipment.ExistingEquipment.FirstOrDefault(ex => ex.Id == ee.Id);
                if (curEquipment == null || ee.Id == 0)
                {
                    dbEquipment.ExistingEquipment.Add(ee);
                    updated = true;
                }
                else
                {
                    ee.EquipmentInfoId = curEquipment.EquipmentInfoId;
                    _dbContext.ExistingEquipment.AddOrUpdate(ee);
                    updated |= _dbContext.Entry(curEquipment).State != EntityState.Unchanged;
                }
            });
            return updated;
        }

        private bool AddOrUpdateInstallationPackages(EquipmentInfo dbEquipment, IEnumerable<InstallationPackage> installationPackages)
        {
            var updated = false;
            var existingEntities =
                dbEquipment.InstallationPackages.Where(
                    a => installationPackages.Any(ee => ee.Id == a.Id)).ToList();
            var entriesForDelete = dbEquipment.InstallationPackages.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => _dbContext.InstallationPackages.Remove(e));

            installationPackages.ForEach(ip =>
            {
                var curPackage =
                    dbEquipment.InstallationPackages.FirstOrDefault(dbp => dbp.Id == ip.Id);
                if (curPackage == null || ip.Id == 0)
                {
                    ip.EquipmentInfo = dbEquipment;
                    dbEquipment.InstallationPackages.Add(ip);
                    updated = true;
                }
                else
                {
                    ip.EquipmentInfoId = dbEquipment.Id;
                    ip.Id = curPackage.Id;
                    _dbContext.InstallationPackages.AddOrUpdate(ip);
                    updated |= _dbContext.Entry(curPackage).State != EntityState.Unchanged;
                }
            });
            return updated;
        }

        private bool AddOrUpdateCustomerLocations(Customer customer, IEnumerable<Location> locations)
        {
            bool updated = false;
            //??
            locations = locations.ToList();
            var existingEntities =
                customer.Locations.Where(
                    a => locations.Any(ca => ca.Id == a.Id || ca.AddressType == a.AddressType)).ToList();
            var entriesForDelete = customer.Locations.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

            locations.ForEach(addr =>
            {
                var curAddress =
                    customer.Locations.FirstOrDefault(ca => ca.AddressType == addr.AddressType);
                if (curAddress == null)
                {
                    addr.Customer = customer;
                    customer.Locations.Add(addr);
                    updated = true;
                }
                else
                {
                    addr.Customer = customer;
                    addr.Id = curAddress.Id;
                    _dbContext.Locations.AddOrUpdate(addr);
                    updated |= _dbContext.Entry(addr).State != EntityState.Unchanged;
                }                
            });

            return updated;
        }

        private bool AddOrUpdateCustomerPhones(Customer customer, IList<Phone> phones)
        {
            bool updated = false;
            var existingEntities =
                customer.Phones.Where(
                    p => phones.Any(pa => pa.Id == p.Id || pa.PhoneType == p.PhoneType)).ToList();
            var entriesForDelete = customer.Phones.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

            phones.ForEach(phone =>
            {
                var curPhone =
                    customer.Phones.FirstOrDefault(p => p.PhoneType == phone.PhoneType);
                if (curPhone == null)
                {
                    phone.Customer = customer;
                    customer.Phones.Add(phone);
                    updated = true;
                }
                else
                {
                    curPhone.PhoneNum = phone.PhoneNum;
                    updated |= _dbContext.Entry(curPhone).State != EntityState.Unchanged;
                }                
            });

            return updated;
        }

        private bool AddOrUpdateCustomerEmails(Customer customer, IList<Email> emails)
        {
            bool updated = false;
            var existingEntities =
                customer.Emails.Where(
                    e => emails.Any(em => e.Id == em.Id || e.EmailType == em.EmailType)).ToList();
            var entriesForDelete = customer.Emails.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);            
            
            emails.ForEach(email =>
            {
                var curEmail =
                    customer.Emails.FirstOrDefault(e => e.EmailType == email.EmailType);
                if (curEmail == null)
                {
                    email.Customer = customer;
                    customer.Emails.Add(email);
                    updated = true;
                }
                else
                {
                    curEmail.EmailAddress = email.EmailAddress;                    
                    updated |= _dbContext.Entry(curEmail).State != EntityState.Unchanged;
                }                
            });

            return updated;
        }

        private void AddOrUpdateContactDetails(Contract contract, ContractDetails contractDetails)
        {
            if (contract.Details == null)
            {
                contract.Details = new ContractDetails();
            }

            if (contractDetails.AgreementType.HasValue)
            {
                contract.Details.AgreementType = contractDetails.AgreementType;
                if (contract.Equipment != null)
                {
                    contract.Equipment.AgreementType = contractDetails.AgreementType.Value;                    
                }
            }
            if (contractDetails.SignatureDocumentId != null)
            {
                contract.Details.SignatureDocumentId = contractDetails.SignatureDocumentId;
            }
            if (contractDetails.SignatureTransactionId != null)
            {
                contract.Details.SignatureTransactionId = contractDetails.SignatureTransactionId;
            }
            if (contractDetails.Status != null)
            {
                contract.Details.Status = contractDetails.Status;
            }
            if (contractDetails.TransactionId != null)
            {
                contract.Details.TransactionId = contractDetails.TransactionId;
            }
            if (contractDetails.SignatureInitiatedTime.HasValue)
            {
                contract.Details.SignatureInitiatedTime = contractDetails.SignatureInitiatedTime;
            }
            if (contractDetails.SignatureStatus.HasValue)
            {
                contract.Details.SignatureStatus = contractDetails.SignatureStatus;
            }
            if (contractDetails.SignatureLastUpdateTime.HasValue)
            {
                contract.Details.SignatureLastUpdateTime = contractDetails.SignatureLastUpdateTime;
            }
            if (contractDetails.ScorecardPoints.HasValue)
            {
                contract.Details.ScorecardPoints = contractDetails.ScorecardPoints;
            }
            if (contractDetails.CreditAmount.HasValue)
            {
                contract.Details.CreditAmount = contractDetails.CreditAmount;
            }
            contract.Details.HouseSize = contractDetails.HouseSize;
            contract.Details.Notes = contractDetails.Notes;
        }

        private void AddOrUpdatePaymentInfo(Contract contract, PaymentInfo newData)
        {
            if (contract.PaymentInfo != null)
            {
                contract.PaymentInfo.PaymentType = newData.PaymentType;
                contract.PaymentInfo.PrefferedWithdrawalDate = newData.PrefferedWithdrawalDate;
                contract.PaymentInfo.AccountNumber = newData.AccountNumber;
                contract.PaymentInfo.BlankNumber = newData.BlankNumber;
                contract.PaymentInfo.TransitNumber = newData.TransitNumber;
                contract.PaymentInfo.EnbridgeGasDistributionAccount = newData.EnbridgeGasDistributionAccount;
                contract.PaymentInfo.MeterNumber = newData.MeterNumber;
            }
            else
            {
                contract.PaymentInfo = newData;
            }
        }

        private void AddOrUpdateEmploymentInfo(Customer customer, EmploymentInfo employmentInfo)
        {
            if (customer.EmploymentInfo == null)
            {
                employmentInfo.Customer = customer;
                if (employmentInfo.CompanyAddress == null)
                {
                    employmentInfo.CompanyAddress = new Address();
                }
                _dbContext.EmploymentInfoes.AddOrUpdate(employmentInfo);
            }
            else
            {
                customer.EmploymentInfo.MonthlyMortgagePayment = employmentInfo.MonthlyMortgagePayment;
                customer.EmploymentInfo.AnnualSalary = employmentInfo.AnnualSalary;
                customer.EmploymentInfo.CompanyName = employmentInfo.CompanyName;
                customer.EmploymentInfo.CompanyPhone = employmentInfo.CompanyPhone;
                customer.EmploymentInfo.EmploymentStatus = employmentInfo.EmploymentStatus;
                customer.EmploymentInfo.EmploymentType = employmentInfo.EmploymentType;
                customer.EmploymentInfo.HourlyRate = employmentInfo.HourlyRate;
                customer.EmploymentInfo.IncomeType = employmentInfo.IncomeType;
                customer.EmploymentInfo.JobTitle = employmentInfo.JobTitle;
                customer.EmploymentInfo.LengthOfEmployment = employmentInfo.LengthOfEmployment;
                if (customer.EmploymentInfo.CompanyAddress == null)
                {
                    customer.EmploymentInfo.CompanyAddress = new Address();
                }
                if (employmentInfo.CompanyAddress != null)
                {                    
                    customer.EmploymentInfo.CompanyAddress.City = employmentInfo.CompanyAddress.City;
                    customer.EmploymentInfo.CompanyAddress.PostalCode = employmentInfo.CompanyAddress.PostalCode;
                    customer.EmploymentInfo.CompanyAddress.State = employmentInfo.CompanyAddress.State;
                    customer.EmploymentInfo.CompanyAddress.Street = employmentInfo.CompanyAddress.Street;
                    customer.EmploymentInfo.CompanyAddress.Unit = employmentInfo.CompanyAddress.Unit;
                }
            }
        }

        private void AddOrUpdateCustomerCreditReport(Customer customer, CustomerCreditReport creditReport)
        {
            creditReport.Customer = customer;
            _dbContext.CustomerCreditReports.AddOrUpdate(creditReport);
        }

        private Customer AddOrUpdateCustomer(Customer customer)
        {
            var dbCustomer = customer.Id == 0 ? null : _dbContext.Customers.Find(customer.Id);
            if (dbCustomer == null)
            {
                customer.Phones = new List<Phone>();
                customer.Locations = new List<Location>();
                customer.Emails = new List<Email>();
                dbCustomer = _dbContext.Customers.Add(customer);
            }
            else
            {
                dbCustomer.FirstName = customer.FirstName;
                dbCustomer.LastName = customer.LastName;
                dbCustomer.DateOfBirth = customer.DateOfBirth;
                dbCustomer.Sin = customer.Sin;
                dbCustomer.DriverLicenseNumber = customer.DriverLicenseNumber;
                dbCustomer.VerificationIdName = customer.VerificationIdName;
                dbCustomer.DealerInitial = customer.DealerInitial;
                dbCustomer.RelationshipToMainBorrower = customer.RelationshipToMainBorrower; 
            }

            if (customer.EmploymentInfo != null)
            {
                AddOrUpdateEmploymentInfo(dbCustomer, customer.EmploymentInfo);
            }

            if (customer.CreditReport != null)
            {
                AddOrUpdateCustomerCreditReport(dbCustomer, customer.CreditReport);
            }

            return dbCustomer;
        }

        private Customer DeleteCustomer(int customerId)
        {
            var customer = GetCustomer(customerId);
            customer.Phones.ForEach(p => customer.Phones.Remove(p));
            customer.Locations.ForEach(l => customer.Locations.Remove(l));
            customer.Emails.ForEach(e => customer.Emails.Remove(e));
            customer = _dbContext.Customers.Remove(customer);
            return customer;
        }

        private bool AddOrUpdateAdditionalApplicants(Contract contract, IList<Customer> customers)
        {
            var updated = false;
            var existingEntities =
                contract.SecondaryCustomers.Where(
                    ho => customers.Any(cho => cho.Id == ho.Id) || ho.IsDeleted == true /*|| (contract.WasDeclined == true && contract.InitialCustomers.Any(ic => ic.Id == ho.Id))*/).ToList();

            var entriesForDelete = contract.SecondaryCustomers.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e =>
            {
                e.IsDeleted = true;
            });            
            customers.ForEach(ho =>
            {
                var customer = UpdateCustomer(ho);
                if (existingEntities.Find(e => e.Id == customer.Id) == null)
                {
                    contract.SecondaryCustomers.Add(customer);
                    updated = true;
                }
            });

            return updated;
        }

        private bool AddOrUpdateHomeOwners(Contract contract, IList<Customer> homeOwners)
        {
            var updated = false;
            var existingEntities =
                contract.HomeOwners.Where(
                    ho => homeOwners.Any(cho => cho.Id == ho.Id) || (contract.WasDeclined == true && contract.InitialCustomers.Any(ic => ic.Id == ho.Id))).ToList();            
            var entriesForDelete = contract.HomeOwners.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            entriesForDelete.ForEach(e => contract.HomeOwners.Remove(e));

            var entriesForAdd = homeOwners.Where(ho => ho.Id == 0 || existingEntities.All(ee => ee.Id != ho.Id));

            entriesForAdd.ForEach(ho =>
            {
                Customer sc = null;
                if (ho.Id != 0)
                {
                    sc = _dbContext.Customers.Find(ho.Id);
                }
                // new created
                if (sc == null && ho.Id == 0)
                {
                    if (contract.PrimaryCustomer.FirstName == ho.FirstName &&
                        contract.PrimaryCustomer.LastName == ho.LastName &&
                        contract.PrimaryCustomer.DateOfBirth == ho.DateOfBirth)
                    {
                        sc = contract.PrimaryCustomer;
                    }
                    else
                    {
                        sc =
                            contract.SecondaryCustomers.FirstOrDefault(
                                c =>
                                    c.FirstName == ho.FirstName && c.LastName == ho.LastName &&
                                    c.DateOfBirth == ho.DateOfBirth &&
                                    c.IsDeleted != true);
                    }
                }
                if (sc != null && (contract.WasDeclined != true || contract.InitialCustomers.All(ic => ic.Id != sc.Id)))
                {
                    contract.HomeOwners.Add(sc);
                    updated = true;
                }
            });

            return updated;
        }

        private bool AddOrUpdateInitialCustomers(Contract contract)
        {
            bool updated = false;
            var currentCustomers = new List<Customer>();
            if (contract.PrimaryCustomer != null)
            {
                currentCustomers.Add(contract.PrimaryCustomer);
            }
            if (contract.SecondaryCustomers?.Any() ?? false)
            {
                currentCustomers.AddRange(contract.SecondaryCustomers);
            }

            var existingEntities =
                contract.InitialCustomers.Where(
                    ic => currentCustomers.Any(cc => cc == ic)).ToList();
            var entriesForDelete = contract.InitialCustomers.Except(existingEntities).ToList();
            updated = entriesForDelete.Any();
            var entriesForAdd = currentCustomers.Except(contract.InitialCustomers).ToList();
            updated = entriesForAdd.Any();
            entriesForDelete.ForEach(e => contract.InitialCustomers.Remove(e));
            entriesForAdd.ForEach(e => contract.InitialCustomers.Add(e));

            return updated;
        }
        #endregion

    }
}
