using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoMapper;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Contract.EquipmentInformation;
using DealnetPortal.Api.Models.DealerOnboarding;
using DealnetPortal.Api.Models.Profile;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Api.Models.UserSettings;
using DealnetPortal.Aspire.Integration.Models.AspireDb;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Utilities.Configuration;
using Unity.Interception.Utilities;
using Contract = DealnetPortal.Domain.Contract;

namespace DealnetPortal.Api.App_Start
{   

    public static class AutoMapperConfig
    {
        public static void Configure()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AllowNullCollections = true;
                MapDomainsToModels(cfg);
                MapAspireDomainsToModels(cfg);
                MapModelsToDomains(cfg);
            });
        }

        private static void MapDomainsToModels(IMapperConfigurationExpression mapperConfig)
        {
            var configuration = new AppConfiguration(WebConfigSections.AdditionalSections);
            var creditReviewStates = configuration.GetSetting(WebConfigKeys.CREDIT_REVIEW_STATUS_CONFIG_KEY)?.Split(',').Select(s => s.Trim()).ToArray();
            var riskBasedStatus = configuration.GetSetting(WebConfigKeys.RISK_BASED_STATUS_KEY)?.Split(',').Select(s => s.Trim()).ToArray();
            var hidePreapprovalAmountForLeaseDealers = false;
            bool.TryParse(configuration.GetSetting(WebConfigKeys.HIDE_PREAPPROVAL_AMOUNT_FOR_LEASEDEALERS_KEY),
                out hidePreapprovalAmountForLeaseDealers);
            var leaseType = "Lease";

            mapperConfig.CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(x => x.SubDealers, o => o.Ignore())
                .ForMember(x => x.UdfSubDealers, d => d.Ignore());
            mapperConfig.CreateMap<GenericSubDealer, SubDealerDTO>();
            mapperConfig.CreateMap<Location, LocationDTO>();
            mapperConfig.CreateMap<Phone, PhoneDTO>()
                .ForMember(x => x.CustomerId, o => o.MapFrom(src => src.Customer != null ? src.Customer.Id : 0));
            mapperConfig.CreateMap<Email, EmailDTO>()
                .ForMember(x => x.CustomerId, o => o.MapFrom(src => src.Customer != null ? src.Customer.Id : 0));
            mapperConfig.CreateMap<EquipmentInfo, EquipmentInfoDTO>()
                .ForMember(x => x.NewEquipment, d => d.ResolveUsing(src => src.NewEquipment?.Where(ne => ne.IsDeleted != true)))
                .ForMember(x => x.IsCustomRateCard, d => d.MapFrom(src => src.IsCustomRateCard))
                .ForMember(x => x.IsFeePaidByCutomer, d => d.MapFrom(src => src.IsFeePaidByCutomer));
            mapperConfig.CreateMap<ExistingEquipment, ExistingEquipmentDTO>();
            mapperConfig.CreateMap<NewEquipment, NewEquipmentDTO>()
                .ForMember(x => x.TypeDescription, d => d.Ignore());
            mapperConfig.CreateMap<InstallationPackage, InstallationPackageDTO>();
            mapperConfig.CreateMap<Comment, CommentDTO>()
                .ForMember(x => x.IsOwn, s => s.ResolveUsing(src => src.IsCustomerComment != true && (src.DealerId == src.Contract?.DealerId)))
                .ForMember(x => x.Replies, s => s.MapFrom(src => src.Replies))
                .ForMember(d => d.AuthorName, s => s.ResolveUsing(src => 
                    src.IsCustomerComment != true ? src.Dealer.UserName : $"{src.Contract?.PrimaryCustomer?.FirstName} {src.Contract?.PrimaryCustomer?.LastName}"));
            mapperConfig.CreateMap<CustomerCreditReport, CustomerCreditReportDTO>()
                .ForMember(x => x.BeaconUpdated, d => d.UseValue(false))
                .ForMember(x => x.EscalatedLimit, d => d.Ignore())
                .ForMember(x => x.NonEscalatedLimit, d => d.Ignore())
                .ForMember(x => x.CreditAmount, d => d.Ignore());
            mapperConfig.CreateMap<Customer, CustomerDTO>()
                .ForMember(x => x.IsHomeOwner, d => d.Ignore())
                .ForMember(x => x.IsInitialCustomer, d => d.Ignore());
            mapperConfig.CreateMap<EmploymentInfo, EmploymentInfoDTO>();
            mapperConfig.CreateMap<ContractSalesRepInfo, ContractSalesRepInfoDTO>();
            mapperConfig.CreateMap<PaymentInfo, PaymentInfoDTO>();
            mapperConfig.CreateMap<ContractDetails, ContractDetailsDTO>()                
                .ForMember(d => d.LocalizedStatus, s => s.ResolveUsing(src => !string.IsNullOrEmpty(src.Status) ? 
                    ResourceHelper.GetGlobalStringResource("_" + src.Status
                        .Replace('-', '_')
                        .Replace(" ", string.Empty)
                        .Replace("$", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)) ?? src.Status : null))
                .ForMember(d => d.SignatureInitiatedTime, s => s.ResolveUsing(src => src.SignatureInitiatedTime))
                .ForMember(d => d.SignatureLastUpdateTime, s => s.ResolveUsing(src => src.SignatureLastUpdateTime));
            mapperConfig.CreateMap<ContractSigner, ContractSignerDTO>()
                .ForMember(d => d.StatusLastUpdateTime, s => s.ResolveUsing(src => src.StatusLastUpdateTime));
            mapperConfig.CreateMap<ContractSigner, SignatureUser>()
                .ForMember(x => x.Role, o => o.MapFrom(src => src.SignerType));
            mapperConfig.CreateMap<Contract, ContractDTO>()
                .ForMember(x => x.PrimaryCustomer, o => o.MapFrom(src => src.PrimaryCustomer))
                .ForMember(x => x.SecondaryCustomers, o => o.ResolveUsing(src => src.SecondaryCustomers?.Where(sc => sc.IsDeleted != true)))
                .ForMember(x => x.PaymentInfo, o => o.MapFrom(src => src.PaymentInfo))
                .ForMember(x => x.Comments, o => o.MapFrom(src => src.Comments))
                .ForMember(x => x.DealerName, o => o.MapFrom(src => src.Dealer.DisplayName))
                .ForMember(x => x.OnCreditReview, o => o.Ignore())
                .AfterMap((c, d) =>
                {
                    if (d?.PrimaryCustomer != null)
                    {
                        d.PrimaryCustomer.IsHomeOwner = c.HomeOwners?.Any(ho => ho.Id == d.PrimaryCustomer.Id) ?? false;
                        d.PrimaryCustomer.IsInitialCustomer =
                            c.InitialCustomers?.Any(ho => ho.Id == d.PrimaryCustomer.Id) ?? false;
                    }
                    d?.SecondaryCustomers?.ForEach(sc =>
                    {
                        sc.IsHomeOwner = c.HomeOwners?.Any(ho => ho.Id == sc.Id) ?? false;
                        sc.IsInitialCustomer = c.InitialCustomers?.Any(ho => ho.Id == sc.Id) ?? false;
                    });
                    if (!string.IsNullOrEmpty(c.Details?.Notes) && d.Details != null)
                    {
                        d.Details.Notes = c.Details?.Notes;
                    }
                    if (!string.IsNullOrEmpty(c.Equipment?.SalesRep))
                    {
                        d.Equipment.SalesRep = c.Equipment?.SalesRep;
                    }
                    if (creditReviewStates?.Any() == true && !string.IsNullOrEmpty(c.Details?.Status))
                    {
                        d.OnCreditReview = creditReviewStates.Contains(c.Details?.Status);
                    }
                    if (d?.PrimaryCustomer?.CreditReport?.CreditLastUpdateTime != null)
                    {
                        d.PrimaryCustomer.CreditReport.BeaconUpdated =
                            d.PrimaryCustomer.CreditReport.CreditLastUpdateTime > d.LastUpdateTime;
                    }
                    if ((c.Dealer?.Tier?.IsCustomerRisk == true || c.IsCreatedByBroker == true || c.IsCreatedByCustomer == true ) 
                        && (!hidePreapprovalAmountForLeaseDealers || c.Dealer?.DealerType != leaseType) && c.Details.CreditAmount > 0 && riskBasedStatus?.Any() == true)
                    {
                        if (riskBasedStatus.Contains(c.Details.Status))
                        {
                            if (CultureInfo.CurrentCulture.Name == "fr")
                            {           
                                d.Details.LocalizedStatus += $" {(double) (c.Details.CreditAmount ?? 0m)}";
                            }
                            else{
                                d.Details.LocalizedStatus += $" {(double) (c.Details.CreditAmount ?? 0m) / 1000} K";
                            }
                        }
                    }
                });
            mapperConfig.CreateMap<Contract, SignatureSummaryDTO>()
                .ForMember(x => x.ContractId, d => d.MapFrom(src => src.Id))
                .ForMember(x => x.SignatureTransactionId, d => d.MapFrom(src => src.Details.SignatureTransactionId))
                .ForMember(x => x.Status, d => d.MapFrom(src => src.Details.SignatureStatus))
                .ForMember(x => x.StatusQualifier, d => d.MapFrom(src => src.Details.SignatureStatusQualifier))
                .ForMember(x => x.StatusTime, d => d.ResolveUsing(src => src.Details.SignatureLastUpdateTime));
            mapperConfig.CreateMap<EquipmentType, EquipmentTypeDTO>().
                ForMember(x => x.Description,s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.DescriptionResource) ?? src.Description));
            mapperConfig.CreateMap<EquipmentSubType, EquipmentSubTypeDTO>().
                ForMember(x => x.Description, s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.DescriptionResource) ?? src.Description));
            mapperConfig.CreateMap<ProvinceTaxRate, ProvinceTaxRateDTO>().
                ForMember(x => x.Description,
                    s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.Description) ?? src.Description));

            mapperConfig.CreateMap<VerifiactionId, VarificationIdsDTO>()
                .ForMember(x => x.VerificationIdName, s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.VerificationIdNameResource) ?? src.VerificationIdName));
                
            mapperConfig.CreateMap<DocumentType, DocumentTypeDTO>().
                ForMember(x => x.Description,
                    s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.DescriptionResource) ?? src.Description));
            mapperConfig.CreateMap<ContractDocument, ContractDocumentDTO>()
                .ForMember(x => x.DocumentBytes, d => d.Ignore());

            mapperConfig.CreateMap<SettingValue, StringSettingDTO>()
                .ForMember(x => x.Name, d => d.ResolveUsing(src => src.Item?.Name))
                .ForMember(x => x.Value, d => d.MapFrom(s => s.StringValue));

            mapperConfig.CreateMap<CustomerLink, CustomerLinkDTO>()
                .ForMember(x => x.EnabledLanguages,
                    d =>
                        d.ResolveUsing(
                            src => src.EnabledLanguages?.Select(l => l.LanguageId).Cast<LanguageCode>().ToList()))
                .ForMember(x => x.HashLink, d => d.MapFrom(s => s.HashLink))
                .ForMember(x => x.Services,
                    d =>
                        d.ResolveUsing(
                            src =>
                                src.Services?.GroupBy(k => k.LanguageId)
                                    .ToDictionary(ds => (LanguageCode) ds.Key, ds => ds.Select(s => s.Service).ToList())));
            mapperConfig.CreateMap<RateCard, RateCardDTO>()
                .ForMember(x => x.Id, d => d.MapFrom(s => s.Id))
                .ForMember(x => x.AdminFee, d => d.MapFrom(s => s.AdminFee))
                .ForMember(x => x.AmortizationTerm, d => d.MapFrom(s => s.AmortizationTerm))
                .ForMember(x => x.CardType, d => d.MapFrom(s => s.CardType))
                .ForMember(x => x.CustomerRate, d => d.MapFrom(s => s.CustomerRate))
                .ForMember(x => x.DealerCost, d => d.MapFrom(s => s.DealerCost))
                .ForMember(x => x.DeferralPeriod, d => d.MapFrom(s => s.DeferralPeriod))
                .ForMember(x => x.LoanTerm, d => d.MapFrom(s => s.LoanTerm))
                .ForMember(x => x.LoanValueFrom, d => d.MapFrom(s => s.LoanValueFrom))
                .ForMember(x => x.LoanValueTo, d => d.MapFrom(s => s.LoanValueTo))
                .ForMember(x => x.ValidFrom, d => d.MapFrom(s => s.ValidFrom))
                .ForMember(x => x.ValidTo, d => d.MapFrom(s => s.ValidTo))
                .ForMember(x => x.IsPromo, d => d.MapFrom(s => s.IsPromo));
            mapperConfig.CreateMap<Tier, TierDTO>()
                .ForMember(x => x.Id, d => d.MapFrom(s => s.Id))
                .ForMember(x => x.Name, d => d.MapFrom(s => s.Name))
                .ForMember(x => x.RateCards, d => d.MapFrom(s => s.RateCards))
                .ForMember(x => x.PassAdminFee, d => d.MapFrom(s => s.PassAdminFee ?? false))
                .ForMember(x => x.IsCustomerRisk, d => d.MapFrom(s => s.IsCustomerRisk ?? false));
            mapperConfig.CreateMap<CustomerRiskGroup, CustomerRiskGroupDTO>()
                .ForMember(x => x.GroupName, s => s.ResolveUsing(src => ResourceHelper.GetGlobalStringResource(src.GroupName.Replace(" ", "")) ?? src.GroupName));
            mapperConfig.CreateMap<DealerEquipment, DealerEquipmentDTO>()
                .ForMember(x => x.Equipment, d => d.MapFrom(src => src.Equipment))
                .ForMember(x => x.ProfileId, d => d.MapFrom(src => src.ProfileId));
            mapperConfig.CreateMap<DealerArea, DealerAreaDTO>();
            mapperConfig.CreateMap<DealerProfile, DealerProfileDTO>()
                .ForMember(x => x.Id, d => d.MapFrom(src => src.Id))
                .ForMember(x => x.DealerId, d => d.MapFrom(src => src.DealerId))
                .ForMember(x => x.EquipmentList, d => d.ResolveUsing(src => src.Equipments.Any() ? src.Equipments : null))
                .ForMember(x => x.PostalCodesList, d => d.ResolveUsing(src => src.Areas.Any() ? src.Areas : null));
            mapperConfig.CreateMap<ProvinceTaxRate, ProvinceTaxRateDTO>();
            mapperConfig.CreateMap<RateReductionCard, RateReductionCardDTO>();

            mapperConfig.CreateMap<Address, AddressDTO>();
            mapperConfig.CreateMap<CompanyInfo, CompanyInfoDTO>()
                .ForMember(x => x.CompanyAddress, d => d.MapFrom(src => src.CompanyAddress))
                .ForMember(x => x.Provinces, d => d.ResolveUsing(src =>
                                                src.Provinces?.Select(p => p.Province).ToList()));
            mapperConfig.CreateMap<ProductInfo, ProductInfoDTO>()
                .ForMember(x => x.Brands, d => d.ResolveUsing(src =>
                                                src.Brands?.Select(b => b.Brand).ToList()))
                .ForMember(x => x.ServiceTypes, d => d.ResolveUsing(src => src.Services?.Select(s => s.Equipment).ToList()));
            mapperConfig.CreateMap<OwnerInfo, OwnerInfoDTO>()
                .ForMember(x => x.Address, d => d.MapFrom(src => src.Address));
            mapperConfig.CreateMap<RequiredDocument, RequiredDocumentDTO>()
                .ForMember(x => x.DocumentBytes, d => d.Ignore())
                .ForMember(x => x.LeadSource, d => d.Ignore());
            mapperConfig.CreateMap<DealerInfo, DealerInfoDTO>()
                .ForMember(x => x.CompanyInfo, d => d.MapFrom(src => src.CompanyInfo))
                .ForMember(x => x.ProductInfo, d => d.MapFrom(src => src.ProductInfo))
                .ForMember(x => x.Owners, d => d.MapFrom(src => src.Owners))
                .ForMember(x => x.RequiredDocuments, d => d.MapFrom(src => src.RequiredDocuments))
                .ForMember(x => x.AdditionalDocuments, d => d.MapFrom(src => src.AdditionalDocuments))
                .ForMember(x => x.SalesRepLink, d => d.Ignore())
                .ForMember(x => x.LeadSource, d => d.Ignore());
            mapperConfig.CreateMap<LicenseType, LicenseTypeDTO>();
            mapperConfig.CreateMap<LicenseDocument, LicenseDocumentDTO>();
            mapperConfig.CreateMap<AdditionalDocument, AdditionalDocumentDTO>();
        }

        private static void MapAspireDomainsToModels(IMapperConfigurationExpression mapperConfig)
        {
            mapperConfig.CreateMap<Aspire.Integration.Models.AspireDb.Contract, ContractDTO>()
                .ForMember(d => d.Id, s => s.UseValue(0))
                .ForMember(d => d.LastUpdateTime, s => s.MapFrom(src => src.LastUpdateDateTime))
                .ForMember(d => d.CreationTime, s => s.MapFrom(src => src.LastUpdateDateTime))
                .ForMember(d => d.Details, s => s.ResolveUsing(src =>
                {
                    var details = new ContractDetailsDTO()
                    {
                        TransactionId = src.TransactionId.ToString(),
                        LocalizedStatus = !string.IsNullOrEmpty(src.DealStatus) ?
                            ResourceHelper.GetGlobalStringResource("_" + src.DealStatus
                                                                       .Replace('-', '_')
                                                                       .Replace(" ", string.Empty)
                                                                       .Replace("$", string.Empty)
                                                                       .Replace("/", string.Empty)
                                                                       .Replace("(", string.Empty)
                                                                       .Replace(")", string.Empty)) ?? src.DealStatus : null,
                        Status = src.DealStatus,
                        CreditAmount = src.OverrideCreditAmountLimit,
                        OverrideCustomerRiskGroup = src.OverrideCustomerRiskGroup,
                        AgreementType =
                            src.AgreementType == "RENTAL"
                                ? AgreementType.RentalApplication
                                : AgreementType.LoanApplication
                    };
                    return details;
                }))
                .ForMember(d => d.Equipment, s => s.ResolveUsing(src =>
                {
                    var equipment = new EquipmentInfoDTO()
                    {
                        Id = 0,
                        LoanTerm = src.Term,
                        RequestedTerm = src.Term,
                        ValueOfDeal = src.AmountFinanced,
                        AgreementType =
                            src.AgreementType == "RENTAL"
                                ? AgreementType.RentalApplication
                                : AgreementType.LoanApplication,
                        NewEquipment = new List<NewEquipmentDTO>()
                        {
                            new NewEquipmentDTO()
                            {
                                Id = 0,
                                Description = src.EquipmentDescription,
                                Type = src.EquipmentType,
                            }
                        }
                    };
                    return equipment;
                }))
                .ForMember(d => d.PrimaryCustomer, s => s.ResolveUsing(src =>
                {
                    var primaryCustomer = new CustomerDTO()
                    {
                        Id = 0,
                        AccountId = src.CustomerAccountId,
                        LastName = src.CustomerLastName,
                        FirstName = src.CustomerFirstName,
                    };
                    return primaryCustomer;
                }))                
                .ForMember(d => d.DealerId, s => s.Ignore())
                .ForMember(d=> d.DealerName, s => s.Ignore())
                .ForMember(d => d.ContractState, s => s.Ignore())
                .ForMember(d => d.ExternalSubDealerName, s => s.Ignore())
                .ForMember(d => d.ExternalSubDealerId, s => s.Ignore())
                .ForMember(d => d.SecondaryCustomers, s => s.Ignore())
                .ForMember(d => d.PaymentInfo, s => s.Ignore())
                .ForMember(d => d.Comments, s => s.Ignore())
                .ForMember(d => d.Documents, s => s.Ignore())
                .ForMember(d => d.WasDeclined, s => s.Ignore())
                .ForMember(d => d.IsCreatedByCustomer, s => s.Ignore())
                .ForMember(d => d.OnCreditReview, s => s.Ignore())
                .ForMember(d => d.IsNewlyCreated, s => s.Ignore())
                .ForMember(d => d.Signers, s => s.Ignore())
                .ForMember(d => d.SalesRepInfo, s => s.Ignore());

            mapperConfig.CreateMap<Aspire.Integration.Models.AspireDb.Entity, CustomerDTO>()
                .ForMember(d => d.Id, s => s.UseValue(0))
                .ForMember(d => d.AccountId, s => s.MapFrom(src => src.EntityId))
                .ForMember(d => d.Emails, s => s.ResolveUsing(src =>
                {
                    List<EmailDTO> emails = null;
                    if (!string.IsNullOrEmpty(src.EmailAddress))
                    {
                        emails = new List<EmailDTO>()
                        {
                            new EmailDTO()
                            {
                                EmailType = EmailType.Main,
                                EmailAddress = src.EmailAddress
                            }
                        };
                    }
                    return emails;
                }))
                .ForMember(d => d.Locations, s => s.ResolveUsing(src =>
                {
                    if (!string.IsNullOrEmpty(src.PostalCode))
                    {
                        var locations = new List<LocationDTO>()
                        {
                            new LocationDTO()
                            {
                                AddressType = AddressType.MainAddress,
                                City = src.City,
                                State = src.State,
                                PostalCode = src.PostalCode,
                                ResidenceType = ResidenceType.Own,
                                Street = src.Street
                            }
                        };
                        return locations;
                    }
                    return null;
                }))
                .ForMember(d => d.Phones, s => s.ResolveUsing(src =>
                {
                    if (!string.IsNullOrEmpty(src.PhoneNum))
                    {
                        var phones = new List<PhoneDTO>()
                        {
                            new PhoneDTO()
                            {
                                PhoneNum = src.PhoneNum,
                                PhoneType = PhoneType.Home
                            }
                        };
                        return phones;
                    }
                    return null;
                }))
                .ForMember(d => d.Sin, s => s.Ignore())
                .ForMember(d => d.DriverLicenseNumber, s => s.Ignore())
                .ForMember(d => d.AllowCommunicate, s => s.Ignore())
                .ForMember(d => d.IsHomeOwner, s => s.Ignore())
                .ForMember(d => d.IsInitialCustomer, s => s.Ignore())
                .ForMember(d => d.PreferredContactMethod, s => s.Ignore())
                .ForMember(d=> d.VerificationIdName, s=> s.Ignore())
                .ForMember(d=> d.DealerInitial, s=> s.Ignore())
                .ForMember(d => d.EmploymentInfo, s => s.Ignore())
                .ForMember(d => d.CreditReport, s => s.Ignore())
                .ForMember(d => d.RelationshipToMainBorrower, s => s.Ignore());
                
            mapperConfig.CreateMap<Aspire.Integration.Models.AspireDb.Entity, DealerDTO>()
                .IncludeBase<Entity, CustomerDTO>()
                .ForMember(d => d.ParentDealerUserName, s => s.MapFrom(src => src.ParentUserName))
                .ForMember(d => d.FirstName, s => s.MapFrom(src => src.FirstName ?? src.Name))
                .ForMember(d => d.PreferredContactMethod, s => s.Ignore())
                .ForMember(d => d.ProductType, s => s.Ignore())
                .ForMember(d => d.ChannelType, s => s.Ignore())
                .ForMember(d => d.Role, s => s.Ignore())
                .ForMember(d => d.Ratecard, s => s.Ignore())
                .ForMember(d => d.EmploymentInfo, s => s.Ignore());

            mapperConfig.CreateMap<Aspire.Integration.Models.AspireDb.DealerRoleEntity, DealerDTO>()
                .IncludeBase<Entity, DealerDTO>()
                .ForMember(d => d.ParentDealerUserName, s => s.MapFrom(src => src.ParentUserName))
                .ForMember(d => d.FirstName, s => s.MapFrom(src => src.FirstName ?? src.Name))
                .ForMember(d => d.PreferredContactMethod, s => s.Ignore())
                .ForMember(d => d.ProductType, s => s.MapFrom(src => src.ProductType))
                .ForMember(d => d.ChannelType, s => s.MapFrom(src => src.ChannelType))
                .ForMember(d => d.Role, s => s.MapFrom(src => src.Role))
                .ForMember(d => d.Ratecard, s => s.MapFrom(src => src.Ratecard))
                .ForMember(d => d.LeaseRatecard, s => s.MapFrom(src => src.LeaseRatecard))
                .ForMember(d => d.EmploymentInfo, s => s.Ignore());
        }

        private static void MapModelsToDomains(IMapperConfigurationExpression mapperConfig)
        {            
            mapperConfig.CreateMap<LocationDTO, Location>()
                .ForMember(x => x.Customer, s => s.Ignore());
            mapperConfig.CreateMap<PhoneDTO, Phone>()
                .ForMember(x => x.Customer, s => s.Ignore());
            mapperConfig.CreateMap<EmailDTO, Email>()
                .ForMember(x => x.Customer, s => s.Ignore());
            mapperConfig.CreateMap<ContractDetailsDTO, ContractDetails>();
            mapperConfig.CreateMap<EquipmentTypeDTO, EquipmentType>()
                .ForMember(x => x.DescriptionResource, d => d.Ignore());
            mapperConfig.CreateMap<EquipmentSubTypeDTO, EquipmentSubType>()
                .ForMember(x => x.DescriptionResource, d => d.Ignore());
            mapperConfig.CreateMap<ContractSalesRepInfoDTO, ContractSalesRepInfo>()
                .ForMember(x => x.Id, d => d.Ignore())
                .ForMember(x => x.Contract, d => d.Ignore());
            mapperConfig.CreateMap<EquipmentInfoDTO, EquipmentInfo>()
                .ForMember(d => d.Contract, x => x.Ignore())
                .ForMember(d => d.ValueOfDeal, x => x.Ignore())
                .ForMember(d => d.RateCard, x => x.Ignore())
                .ForMember(d => d.IsCustomRateCard, x => x.MapFrom(src => src.IsCustomRateCard ?? false));
            mapperConfig.CreateMap<NewEquipmentDTO, NewEquipment>()
                .ForMember(x => x.EquipmentInfo, d => d.Ignore())
                .ForMember(x => x.EquipmentInfoId, d => d.Ignore())
                .ForMember(x => x.IsDeleted, d => d.Ignore())
                .ForMember(x => x.EquipmentSubType, d => d.Ignore())
                .ForMember(x => x.EquipmentType, d => d.Ignore());
            mapperConfig.CreateMap<ExistingEquipmentDTO, ExistingEquipment>()
                .ForMember(x => x.EquipmentInfo, d => d.Ignore())
                .ForMember(x => x.EquipmentInfoId, d => d.Ignore());
            mapperConfig.CreateMap<InstallationPackageDTO, InstallationPackage>()
                .ForMember(x => x.EquipmentInfo, d => d.Ignore())
                .ForMember(x => x.EquipmentInfoId, d => d.Ignore()); ;
            mapperConfig.CreateMap<CommentDTO, Comment>()
               .ForMember(x => x.ParentComment, d => d.Ignore())
               .ForMember(x => x.Contract, d => d.Ignore())
               .ForMember(x => x.Replies, d => d.Ignore())
               .ForMember(d => d.Dealer, s => s.Ignore());
            mapperConfig.CreateMap<CustomerDTO, Customer>()
                .ForMember(x => x.Sin, s => s.ResolveUsing(src => string.IsNullOrWhiteSpace(src.Sin) ? null : src.Sin))
                .ForMember(x => x.DriverLicenseNumber, s => s.ResolveUsing(src => string.IsNullOrWhiteSpace(src.DriverLicenseNumber) ? null : src.DriverLicenseNumber))
                .ForMember(x => x.PreferredContactMethod, s => s.MapFrom(m => m.PreferredContactMethod))
                .ForMember(x => x.VerificationIdName, s => s.MapFrom(m => m.VerificationIdName))
                .ForMember(x => x.DealerInitial, s => s.MapFrom(m => m.DealerInitial))
                .ForMember(x => x.ExistingCustomer, d => d.Ignore())
                .ForMember(x => x.CreditReport, d => d.Ignore())
                .ForMember(x => x.AccountId, d => d.Ignore())
                .ForMember(x => x.IsDeleted, d => d.Ignore());

            mapperConfig.CreateMap<CustomerInfoDTO, Customer>()
                .ForMember(x => x.AccountId, d => d.Ignore())
                .ForMember(x => x.Locations, d => d.Ignore())
                .ForMember(x => x.Emails, d => d.Ignore())
                .ForMember(x => x.Phones, d => d.Ignore())
                .ForMember(x => x.ExistingCustomer, d => d.Ignore())
                .ForMember(x => x.EmploymentInfo, d => d.Ignore())
                .ForMember(x => x.CreditReport, d => d.Ignore())
                .ForMember(x => x.RelationshipToMainBorrower, d => d.Ignore());

            mapperConfig.CreateMap<EmploymentInfoDTO, EmploymentInfo>()
                .ForMember(x => x.Id, d => d.Ignore())
                .ForMember(x => x.Customer, d => d.Ignore());

            mapperConfig.CreateMap<ContractDataDTO, ContractData>()
                .ForMember(x => x.HomeOwners, d => d.Ignore())
                .AfterMap((d, c, rc) =>
                {                    
                    if (d?.PrimaryCustomer?.IsHomeOwner == true || (d?.SecondaryCustomers?.Any(sc => sc.IsHomeOwner == true) ?? false))
                    {
                        c.HomeOwners = new List<Customer>();
                        if (d?.PrimaryCustomer?.IsHomeOwner == true)
                        {
                            c.HomeOwners.Add(c.PrimaryCustomer);
                        }
                        d?.SecondaryCustomers?.Where(sc => sc.IsHomeOwner == true).ForEach(sc =>
                        {
                            c.HomeOwners.Add(rc.Mapper.Map<Customer>(sc));
                        });
                    }
                });
            mapperConfig.CreateMap<PaymentInfoDTO, PaymentInfo>()
                .ForMember(d => d.Contract, s => s.Ignore());            
            mapperConfig.CreateMap<ContractDTO, Contract>()
                .ForMember(d => d.PaymentInfo, s => s.Ignore())
                .ForMember(d => d.Comments, s => s.Ignore())
                .ForMember(x => x.Documents, d => d.Ignore())
                .ForMember(x => x.Dealer, d => d.Ignore())
                .ForMember(x => x.HomeOwners, d => d.Ignore())
                .ForMember(x => x.InitialCustomers, d => d.Ignore())
                .ForMember(x => x.CreateOperator, d => d.Ignore())
                .ForMember(x => x.LastUpdateOperator, d => d.Ignore())
                .ForMember(x => x.IsCreatedByBroker, d => d.Ignore())
                .ForMember(x => x.Signers, d => d.Ignore())
                .ForMember(x => x.DateOfSubmit, d => d.Ignore());
            
            mapperConfig.CreateMap<DocumentTypeDTO, DocumentType>()
                .ForMember(x => x.DescriptionResource, d => d.Ignore());
            mapperConfig.CreateMap<ContractDocumentDTO, ContractDocument>()
                .ForMember(x => x.Contract, d => d.Ignore())
                .ForMember(x => x.DocumentType, d => d.Ignore());

            mapperConfig.CreateMap<CustomerLinkDTO, CustomerLink>()                
                .ForMember(x => x.EnabledLanguages, d => d.ResolveUsing(src =>
                    src.EnabledLanguages?.Select(l => new DealerLanguage() {LanguageId = (int)l, Language = new Language() { Id = (int)l } }).ToList()))
                .ForMember(x => x.Services, d => d.ResolveUsing(src =>
                    src.Services?.SelectMany(ds => ds.Value.Select(dsv => new DealerService() {LanguageId = (int)ds.Key, Service = dsv}))))
                .ForMember(x => x.Id, d => d.Ignore())
                .ForMember(x => x.HashLink, d => d.MapFrom(s=>s.HashLink));
            mapperConfig.CreateMap<DealerEquipmentDTO, DealerEquipment>()
                .ForMember(x => x.EquipmentId, d => d.ResolveUsing(src => src.Equipment.Id))
                .ForMember(x => x.DealerProfile, d => d.Ignore())
                .ForMember(x => x.Id, d => d.Ignore());
            mapperConfig.CreateMap<DealerAreaDTO, DealerArea>()
                .ForMember(x => x.DealerProfile, d => d.Ignore());
            mapperConfig.CreateMap<DealerProfileDTO, DealerProfile>()
                .ForMember(x => x.Id, d => d.MapFrom( src => src.Id))
                .ForMember(x => x.DealerId, d => d.MapFrom( src => src.DealerId))
                .ForMember(x => x.Equipments, d => d.MapFrom( src => src.EquipmentList.Select( s=> new DealerEquipment() {EquipmentId = s.Equipment.Id, ProfileId = src.Id})))
                .ForMember(x => x.Areas, d => d.MapFrom( src => src.PostalCodesList.Select(s => new DealerArea() {ProfileId = src.Id, PostalCode = s.PostalCode})))
                .ForMember(x => x.Dealer, d => d.Ignore());
            mapperConfig.CreateMap<ProvinceTaxRateDTO, ProvinceTaxRate>()
                .ForMember(x => x.Name, d => d.Ignore());
            mapperConfig.CreateMap<SignatureUser, ContractSigner>()
                .ForMember(x => x.SignerType, d => d.MapFrom(src => src.Role))
                .ForMember(x => x.Contract, d => d.Ignore())
                .ForMember(x => x.ContractId, d => d.Ignore())
                .ForMember(x => x.Comment, d => d.Ignore())
                .ForMember(x => x.SignatureStatus, d => d.Ignore())
                .ForMember(x => x.SignatureStatusQualifier, d => d.Ignore())                
                .ForMember(x => x.Contract, d => d.Ignore())
                .ForMember(x => x.Customer, d => d.Ignore())
                .ForMember(x => x.StatusLastUpdateTime, d => d.Ignore());

            mapperConfig.CreateMap<AddressDTO, Address>();
            mapperConfig.CreateMap<CompanyInfoDTO, CompanyInfo>()
                .ForMember(x => x.CompanyAddress, d => d.ResolveUsing(src => src.CompanyAddress))
                .ForMember(x => x.Provinces, d => d.ResolveUsing(src =>
                                                src.Provinces?.Select(p => new CompanyProvince() {Province = p}).ToList()));
            mapperConfig.CreateMap<EquipmentTypeDTO, ProductService>()
                .ForMember(x => x.Id, d => d.Ignore())
                .ForMember(x => x.ProductInfo, d => d.Ignore())
                .ForMember(x => x.ProductInfoId, d => d.Ignore())
                .ForMember(x => x.Equipment, d => d.Ignore())
                .ForMember(x => x.EquipmentId, d => d.MapFrom(src => src.Id));
            mapperConfig.CreateMap<ProductInfoDTO, ProductInfo>()
                .ForMember(x => x.Brands, d => d.ResolveUsing(src =>
                                                src.Brands?.Select(b => new ManufacturerBrand() {Brand = b}).ToList()))
                .ForMember(x => x.Services, d => d.MapFrom(src => src.ServiceTypes));
            mapperConfig.CreateMap<OwnerInfoDTO, OwnerInfo>()
                .ForMember(x => x.Address, d => d.MapFrom(src => src.Address))
                .ForMember(x => x.DealerInfo, d => d.Ignore())
                .ForMember(x => x.DealerInfoId, d => d.Ignore());
            mapperConfig.CreateMap<RequiredDocumentDTO, RequiredDocument>()
                .ForMember(x => x.DealerInfo, d => d.Ignore())
                .ForMember(x => x.Uploaded, d => d.Ignore())
                .ForMember(x => x.UploadDate, d => d.Ignore())
                .ForMember(x => x.Status, d => d.Ignore())
                .ForMember(x => x.DocumentType, d => d.Ignore());
            mapperConfig.CreateMap<DealerInfoDTO, DealerInfo>()
                .ForMember(x => x.CompanyInfo, d => d.MapFrom(src => src.CompanyInfo))
                .ForMember(x => x.ProductInfo, d => d.MapFrom(src => src.ProductInfo))
                .ForMember(x => x.Owners, d => d.MapFrom(src => src.Owners))
                .ForMember(x => x.RequiredDocuments, d => d.MapFrom(src => src.RequiredDocuments))
                .ForMember(x => x.AdditionalDocuments, d => d.MapFrom(src => src.AdditionalDocuments))
                .ForMember(x => x.ParentSalesRep, d => d.Ignore())
                .ForMember(x => x.Status, d => d.Ignore())
                .ForMember(x => x.Submitted, d => d.Ignore())
                .ForMember(x => x.SentToAspire, d => d.Ignore());
            mapperConfig.CreateMap<LicenseTypeDTO, LicenseType>()
                .ForMember(x => x.LicenseDocuments, d => d.Ignore());
            mapperConfig.CreateMap<LicenseDocumentDTO, LicenseDocument>()
                .ForMember(x => x.EquipmentTypeId, d => d.Ignore());
            mapperConfig.CreateMap<AdditionalDocumentDTO, AdditionalDocument>()
                .ForMember(x => x.DealerInfo, d => d.Ignore())
                .ForMember(x => x.License, d => d.Ignore())
                .ForMember(x => x.LicenseTypeId, d => d.MapFrom(src=>src.License.Id));
        }
    }
}