using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Domain.Repositories;
using Unity.Interception.Utilities;

namespace DealnetPortal.DataAccess.Repositories
{
    public class DealerOnboardingRepository : BaseRepository, IDealerOnboardingRepository
    {
        public DealerOnboardingRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public DealerInfo GetDealerInfoById(int id)
        {
            return _dbContext.DealerInfos.
                Find(id);
        }

        public DealerInfo GetDealerInfoByAccessKey(string accessKey)
        {
            return _dbContext.DealerInfos
                .FirstOrDefault(di => di.AccessKey == accessKey);
        }

        public DealerInfo AddOrUpdateDealerInfo(DealerInfo dealerInfo)
        {
            DealerInfo dbDealer = null;

            if (dealerInfo.Id != 0)
            {
                dbDealer = GetDealerInfoById(dealerInfo.Id);
            }
            if (dbDealer == null && !string.IsNullOrEmpty(dealerInfo.AccessKey))
            {
                dbDealer = GetDealerInfoByAccessKey(dealerInfo.AccessKey);
            }
            
            if (dbDealer == null)
            {
                //add new
                dealerInfo.CreationTime = DateTime.UtcNow;
                dealerInfo.LastUpdateTime = DateTime.UtcNow;
                //dealerInfo.ProductInfo?.Services?.ForEach(s => 
                //    s.Equipment = _dbContext.EquipmentTypes.Find(s.EquipmentId));
                dealerInfo.AccessKey = GenerateDealerAccessCode();
                dealerInfo.AdditionalDocuments?.ForEach(d => d.License = _dbContext.LicenseTypes.Find(d.LicenseTypeId));
                dealerInfo.ProductInfo?.Services?.ForEach(s => s.Equipment = _dbContext.EquipmentTypes.Find(s.EquipmentId));
                dbDealer = _dbContext.DealerInfos.Add(dealerInfo);
            }
            else
            {
                //update
                UpdateDealerInfo(dbDealer, dealerInfo);
            }

            return dbDealer;
        }

        public bool DeleteDealerInfo(int dealerInfoId)
        {
            var dealerInfo = _dbContext.DealerInfos.Find(dealerInfoId);
            if (dealerInfo != null)
            {
                return _dbContext.DealerInfos.Remove(dealerInfo) != null;
            }
            return false;
        }

        public bool DeleteDealerInfo(string accessCode)
        {
            var dealerInfo = _dbContext.DealerInfos.FirstOrDefault(d => d.AccessKey == accessCode);
            if (dealerInfo != null)
            {
                return _dbContext.DealerInfos.Remove(dealerInfo) != null;
            }
            return false;
        }

        public RequiredDocument AddDocumentToDealer(int dealerInfoId, RequiredDocument document)
        {
            var dealerInfo = dealerInfoId != 0 ? GetDealerInfoById(dealerInfoId) : CreateDealerInfo();
            return AddRequiredDocument(dealerInfo, document);
        }

        public bool DeleteDocumentFromDealer(int documentId)
        {
            var document = _dbContext.RequiredDocuments.SingleOrDefault(x => x.Id == documentId);
            _dbContext.RequiredDocuments.Remove(document);
            return true;
        }

        public RequiredDocument SetDocumentStatus(int documentId, DocumentStatus newStatus)
        {
            var dbDoc = _dbContext.RequiredDocuments.Find(documentId);
            if (dbDoc != null)
            {
                dbDoc.Status = newStatus;
            }
            return dbDoc;
        }

        public RequiredDocument AddDocumentToDealer(string accessCode, RequiredDocument document)
        {
            var dealerInfo = string.IsNullOrEmpty(accessCode) ? GetDealerInfoByAccessKey(accessCode) : CreateDealerInfo();
            return AddRequiredDocument(dealerInfo, document);
        }

        #region Update Logic

        private DealerInfo CreateDealerInfo()
        {
            var dealerInfo = new DealerInfo()
            {
                CreationTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                AccessKey = GenerateDealerAccessCode()
            };
            _dbContext.DealerInfos.Add(dealerInfo);
            return dealerInfo;
        }

        private string GenerateDealerAccessCode()
        {
            return Guid.NewGuid().ToString();
        }

        private RequiredDocument AddRequiredDocument(DealerInfo dbDealerInfo, RequiredDocument requiredDocument)
        {
            var dbDoc = requiredDocument.Id != 0 ? _dbContext.RequiredDocuments.Find(requiredDocument.Id) : null;
            if (dbDoc?.DealerInfo?.Id != dbDealerInfo.Id)
            {
                requiredDocument.Id = 0;
                requiredDocument.CreationDate = DateTime.UtcNow;
                requiredDocument.Status = DocumentStatus.Adding;
                dbDealerInfo.RequiredDocuments.Add(requiredDocument);
                dbDoc = requiredDocument;
            }
            else
            {
                requiredDocument.CreationDate = DateTime.UtcNow;
                if (requiredDocument.DocumentBytes != null)
                {
                    dbDoc.DocumentBytes = requiredDocument.DocumentBytes;
                }
                if (dbDoc.DocumentName != requiredDocument.DocumentName)
                {
                    dbDoc.DocumentName = requiredDocument.DocumentName;
                }
                if (dbDoc.DocumentTypeId != requiredDocument.DocumentTypeId)
                {
                    dbDoc.DocumentTypeId = requiredDocument.DocumentTypeId;
                }
            }
            dbDoc.Uploaded = false;
            return dbDoc;
        }

        private void UpdateDealerInfo(DealerInfo dbDealerInfo, DealerInfo updateDealerInfo)
        {
            UpdateBaseDealerInfo(dbDealerInfo, updateDealerInfo);
            UpdateDealerCompanyInfo(dbDealerInfo, updateDealerInfo);
            UpdateDealerProductInfo(dbDealerInfo, updateDealerInfo);
            UpdateDealerOwners(dbDealerInfo, updateDealerInfo.Owners);
            UpdateDealerAdditionalDocument(dbDealerInfo, updateDealerInfo.AdditionalDocuments);
            dbDealerInfo.LastUpdateTime = DateTime.UtcNow;
        }

        private void UpdateDealerAdditionalDocument(DealerInfo dbDealerInfo, ICollection<AdditionalDocument> documents)
        {
            if (documents == null)
            {
                dbDealerInfo.AdditionalDocuments.ForEach(e => dbDealerInfo.AdditionalDocuments.Remove(e));
            }
            else
            {
                var newEntities = new List<AdditionalDocument>();
                documents.ForEach(doc =>
                {
                    var dbDoc = dbDealerInfo.AdditionalDocuments.SingleOrDefault(x => x.LicenseTypeId == doc.LicenseTypeId);
                    if (dbDoc != null)
                    {
                        dbDoc.ExpiredDate = doc.ExpiredDate;
                        dbDoc.NotExpired = doc.NotExpired;
                        dbDoc.Number = doc.Number;
                    }
                    else
                    {
                        doc.License = _dbContext.LicenseTypes.Find(doc.LicenseTypeId);
                        doc.DealerInfoId = dbDealerInfo.Id;
                        newEntities.Add(doc);
                        dbDealerInfo.AdditionalDocuments.Add(doc);
                    }
                });
                var existingEntities = dbDealerInfo.AdditionalDocuments.Where(a => documents.Any(ee => ee.LicenseTypeId == a.LicenseTypeId)).ToList();
                var entriesForDelete = dbDealerInfo.AdditionalDocuments.Except(existingEntities).Except(newEntities).ToList();
                entriesForDelete.ForEach(e => _dbContext.AdditionalDocuments.Remove(e));
            }
        }

        private void UpdateBaseDealerInfo(DealerInfo dbDealerInfo, DealerInfo updateDealerInfo)
        {
            if (dbDealerInfo.AccessKey != updateDealerInfo.AccessKey)
            {
                dbDealerInfo.AccessKey = updateDealerInfo.AccessKey;
            }
            if (string.IsNullOrEmpty(dbDealerInfo.AccessKey))
            {
                dbDealerInfo.AccessKey = GenerateDealerAccessCode();
            }
            if (dbDealerInfo.CreditCheckConsent != updateDealerInfo.CreditCheckConsent)
            {
                dbDealerInfo.CreditCheckConsent = updateDealerInfo.CreditCheckConsent;
            }
            if (dbDealerInfo.MarketingConsent != updateDealerInfo.MarketingConsent)
            {
                dbDealerInfo.MarketingConsent = updateDealerInfo.MarketingConsent;
            }
            if (!string.IsNullOrEmpty(updateDealerInfo.ParentSalesRepId) && dbDealerInfo.ParentSalesRepId != updateDealerInfo.ParentSalesRepId)
            {
                dbDealerInfo.ParentSalesRepId = updateDealerInfo.ParentSalesRepId;
            }
        }    

        private void UpdateDealerCompanyInfo(DealerInfo dbDealerInfo, DealerInfo updateDealerInfo)
        {
            if (dbDealerInfo.CompanyInfo == null && updateDealerInfo.CompanyInfo != null)
            {
                dbDealerInfo.CompanyInfo = updateDealerInfo.CompanyInfo;
            }
            else
            if (dbDealerInfo.CompanyInfo != null && updateDealerInfo.CompanyInfo == null)
            {
                _dbContext.CompanyInfos.Remove(dbDealerInfo.CompanyInfo);
            }
            else
            if (dbDealerInfo.CompanyInfo != null && updateDealerInfo.CompanyInfo != null)
            {
                updateDealerInfo.Id = dbDealerInfo.Id;
                var dbCompanyInfo = dbDealerInfo.CompanyInfo;
                var updateCompanyInfo = updateDealerInfo.CompanyInfo;
                if (dbCompanyInfo.FullLegalName != updateCompanyInfo.FullLegalName)
                {
                    dbCompanyInfo.FullLegalName = updateCompanyInfo.FullLegalName;
                }
                if (dbCompanyInfo.EmailAddress != updateCompanyInfo.EmailAddress)
                {
                    dbCompanyInfo.EmailAddress = updateCompanyInfo.EmailAddress;
                }
                if (dbCompanyInfo.OperatingName != updateCompanyInfo.OperatingName)
                {
                    dbCompanyInfo.OperatingName = updateCompanyInfo.OperatingName;
                }
                if (dbCompanyInfo.Phone != updateCompanyInfo.Phone)
                {
                    dbCompanyInfo.Phone = updateCompanyInfo.Phone;
                }
                if (dbCompanyInfo.Website != updateCompanyInfo.Website)
                {
                    dbCompanyInfo.Website = updateCompanyInfo.Website;
                }
                if (dbCompanyInfo.BusinessType != updateCompanyInfo.BusinessType)
                {
                    dbCompanyInfo.BusinessType = updateCompanyInfo.BusinessType;
                }
                if (dbCompanyInfo.NumberOfInstallers != updateCompanyInfo.NumberOfInstallers)
                {
                    dbCompanyInfo.NumberOfInstallers = updateCompanyInfo.NumberOfInstallers;
                }
                if (dbCompanyInfo.NumberOfSales != updateCompanyInfo.NumberOfSales)
                {
                    dbCompanyInfo.NumberOfSales = updateCompanyInfo.NumberOfSales;
                }
                if (dbCompanyInfo.YearsInBusiness != updateCompanyInfo.YearsInBusiness)
                {
                    dbCompanyInfo.YearsInBusiness = updateCompanyInfo.YearsInBusiness;
                }
                if (dbCompanyInfo.CompanyAddress.City != updateCompanyInfo.CompanyAddress?.City)
                {
                    dbCompanyInfo.CompanyAddress.City = updateCompanyInfo.CompanyAddress?.City;
                }
                if (dbCompanyInfo.CompanyAddress.PostalCode != updateCompanyInfo.CompanyAddress?.PostalCode)
                {
                    dbCompanyInfo.CompanyAddress.PostalCode = updateCompanyInfo.CompanyAddress?.PostalCode;
                }
                if (dbCompanyInfo.CompanyAddress.State != updateCompanyInfo.CompanyAddress?.State)
                {
                    dbCompanyInfo.CompanyAddress.State = updateCompanyInfo.CompanyAddress?.State;
                }
                if (dbCompanyInfo.CompanyAddress.Street != updateCompanyInfo.CompanyAddress?.Street)
                {
                    dbCompanyInfo.CompanyAddress.Street = updateCompanyInfo.CompanyAddress?.Street;
                }
                if (dbCompanyInfo.CompanyAddress.Unit != updateCompanyInfo.CompanyAddress?.Unit)
                {
                    dbCompanyInfo.CompanyAddress.Unit = updateCompanyInfo.CompanyAddress?.Unit;
                }

                UpdateCompanyProvinces(dbCompanyInfo, updateCompanyInfo.Provinces);
            }
        }

        private void UpdateCompanyProvinces(CompanyInfo dbCompanyInfo, ICollection<CompanyProvince> updatedProvinces)
        {
            var existingEntities =
                dbCompanyInfo.Provinces.Where(
                    p => updatedProvinces?.Any(up => up.Province == p.Province) ?? false).ToList();
            var entriesForDelete = dbCompanyInfo.Provinces.Except(existingEntities).ToList();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

            updatedProvinces?.Except(updatedProvinces.Where(up => existingEntities.Any(e => e.Province == up.Province))).
                ForEach(p =>
                {
                    p.CompanyInfo = dbCompanyInfo;
                    dbCompanyInfo.Provinces.Add(p);
                });            
        }

        private void UpdateDealerProductInfo(DealerInfo dbDealerInfo, DealerInfo updateDealerInfo)
        {
            if (dbDealerInfo.ProductInfo == null && updateDealerInfo.ProductInfo != null)
            {
                dbDealerInfo.ProductInfo = updateDealerInfo.ProductInfo;
                dbDealerInfo.ProductInfo?.Services?.ForEach(s => s.Equipment = _dbContext.EquipmentTypes.Find(s.EquipmentId));
            }
            else
            if (dbDealerInfo.ProductInfo != null && updateDealerInfo.ProductInfo == null)
            {
                _dbContext.ProductInfos.Remove(dbDealerInfo.ProductInfo);
            }
            else
            if (dbDealerInfo.ProductInfo != null && updateDealerInfo.ProductInfo != null)
            {
                updateDealerInfo.Id = dbDealerInfo.Id;
                var dbProductInfo = dbDealerInfo.ProductInfo;
                var updateProductInfo = updateDealerInfo.ProductInfo;
                //_dbContext.Entry(dbProductInfo).CurrentValues.SetValues();
                if (dbProductInfo.AnnualSalesVolume != updateProductInfo.AnnualSalesVolume)
                {
                    dbProductInfo.AnnualSalesVolume = updateProductInfo.AnnualSalesVolume;
                }
                if (dbProductInfo.AverageTransactionSize != updateProductInfo.AverageTransactionSize)
                {
                    dbProductInfo.AverageTransactionSize = updateProductInfo.AverageTransactionSize;
                }
                if (dbProductInfo.FinanceProviderName != updateProductInfo.FinanceProviderName)
                {
                    dbProductInfo.FinanceProviderName = updateProductInfo.FinanceProviderName;
                }
                if (dbProductInfo.OemName != updateProductInfo.OemName)
                {
                    dbProductInfo.OemName = updateProductInfo.OemName;
                }
                if (dbProductInfo.ProgramService != updateProductInfo.ProgramService)
                {
                    dbProductInfo.ProgramService = updateProductInfo.ProgramService;
                }
                if (dbProductInfo.SalesApproachConsumerDirect != updateProductInfo.SalesApproachConsumerDirect)
                {
                    dbProductInfo.SalesApproachConsumerDirect = updateProductInfo.SalesApproachConsumerDirect;
                }
                if (dbProductInfo.SalesApproachBroker != updateProductInfo.SalesApproachBroker)
                {
                    dbProductInfo.SalesApproachBroker = updateProductInfo.SalesApproachBroker;
                }
                if (dbProductInfo.SalesApproachDistributor != updateProductInfo.SalesApproachDistributor)
                {
                    dbProductInfo.SalesApproachDistributor = updateProductInfo.SalesApproachDistributor;
                }
                if (dbProductInfo.SalesApproachDoorToDoor != updateProductInfo.SalesApproachDoorToDoor)
                {
                    dbProductInfo.SalesApproachDoorToDoor = updateProductInfo.SalesApproachDoorToDoor;
                }

                if (dbProductInfo.LeadGenLocalAdvertising != updateProductInfo.LeadGenLocalAdvertising)
                {
                    dbProductInfo.LeadGenLocalAdvertising = updateProductInfo.LeadGenLocalAdvertising;
                }
                if (dbProductInfo.LeadGenReferrals != updateProductInfo.LeadGenReferrals)
                {
                    dbProductInfo.LeadGenReferrals = updateProductInfo.LeadGenReferrals;
                }
                if (dbProductInfo.LeadGenTradeShows != updateProductInfo.LeadGenTradeShows)
                {
                    dbProductInfo.LeadGenTradeShows = updateProductInfo.LeadGenTradeShows;
                }
                if (dbProductInfo.MonthlyFinancedValue != updateProductInfo.MonthlyFinancedValue)
                {
                    dbProductInfo.MonthlyFinancedValue = updateProductInfo.MonthlyFinancedValue;
                }
                if (dbProductInfo.OfferMonthlyDeferrals != updateProductInfo.OfferMonthlyDeferrals)
                {
                    dbProductInfo.OfferMonthlyDeferrals = updateProductInfo.OfferMonthlyDeferrals;
                }
                if (dbProductInfo.PercentMonthlyDealsDeferred != updateProductInfo.PercentMonthlyDealsDeferred)
                {
                    dbProductInfo.PercentMonthlyDealsDeferred = updateProductInfo.PercentMonthlyDealsDeferred;
                }
                if (dbProductInfo.ReasonForInterest != updateProductInfo.ReasonForInterest)
                {
                    dbProductInfo.ReasonForInterest = updateProductInfo.ReasonForInterest;
                }
                if (dbProductInfo.Relationship != updateProductInfo.Relationship)
                {
                    dbProductInfo.Relationship = updateProductInfo.Relationship;
                }
                if (dbProductInfo.WithCurrentProvider != updateProductInfo.WithCurrentProvider)
                {
                    dbProductInfo.WithCurrentProvider = updateProductInfo.WithCurrentProvider;
                }
                if (dbProductInfo.PrimaryBrand != updateProductInfo.PrimaryBrand)
                {
                    dbProductInfo.PrimaryBrand = updateProductInfo.PrimaryBrand;
                }

                UpdateProductBrands(dbProductInfo, updateProductInfo.Brands);
                UpdateProductServices(dbProductInfo, updateProductInfo.Services);
            }
        }

        private void UpdateProductBrands(ProductInfo dbProductInfo, ICollection<ManufacturerBrand> updatedBrands)
        {
            var existingEntities =
                dbProductInfo.Brands.Where(
                    p => updatedBrands?.Any(up => up.Brand == p.Brand) ?? false).ToList();
            var entriesForDelete = dbProductInfo.Brands.Except(existingEntities).ToList();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

            updatedBrands?.Except(updatedBrands.Where(up => existingEntities.Any(e => e.Brand == up.Brand))).
                ForEach(p =>
                {
                    p.ProductInfo = dbProductInfo;
                    dbProductInfo.Brands.Add(p);
                });
        }

        private void UpdateProductServices(ProductInfo dbProductInfo, ICollection<ProductService> updatedServices)
        {
            var existingEntities =
                dbProductInfo.Services.Where(
                    p => updatedServices?.Any(up => up.EquipmentId == p.EquipmentId || up.Equipment?.Type == p.Equipment?.Type) ?? false).ToList();
            var entriesForDelete = dbProductInfo.Services.Except(existingEntities).ToList();
            entriesForDelete.ForEach(e => _dbContext.Entry(e).State = EntityState.Deleted);

            updatedServices?.Except(updatedServices.Where(up => existingEntities.Any(e => e.EquipmentId == up.EquipmentId))).
                ForEach(p =>
                {
                    if (p.EquipmentId != 0)
                    {
                        p.Equipment = _dbContext.EquipmentTypes.Find(p.EquipmentId);
                    }
                    else if (!string.IsNullOrEmpty(p.Equipment?.Type))
                    {
                        p.Equipment = _dbContext.EquipmentTypes.FirstOrDefault(eqt => eqt.Type == p.Equipment.Type);
                    }
                    p.ProductInfo = dbProductInfo;
                    dbProductInfo.Services.Add(p);
                });            
        }

        private void UpdateDealerOwners(DealerInfo dbDealerInfo, ICollection<OwnerInfo> updatedOwners)
        {
            var existingEntities =
               dbDealerInfo.Owners.Where(
                   ho => updatedOwners?.Any(cho => cho.Id == ho.Id) ?? false).ToList();

            var entriesForDelete = dbDealerInfo.Owners.Except(existingEntities).ToList();
            entriesForDelete.ForEach(e => _dbContext.OwnerInfos.Remove(e));

            updatedOwners.ForEach(ho =>
            {
                var dbCustomer = ho.Id == 0 ? null : _dbContext.OwnerInfos.Find(ho.Id);
                if (dbCustomer == null)
                {
                    ho.DealerInfo = dbDealerInfo;
                    ho.DealerInfoId = dbDealerInfo.Id;
                    dbDealerInfo.Owners.Add(ho);
                }
                else
                {                    
                    if (dbCustomer.DateOfBirth != ho.DateOfBirth)
                    {
                        dbCustomer.DateOfBirth = ho.DateOfBirth;
                    }
                    if (dbCustomer.EmailAddress != ho.EmailAddress)
                    {
                        dbCustomer.EmailAddress = ho.EmailAddress;
                    }
                    if (dbCustomer.FirstName != ho.FirstName)
                    {
                        dbCustomer.FirstName = ho.FirstName;
                    }
                    if (dbCustomer.LastName != ho.LastName)
                    {
                        dbCustomer.LastName = ho.LastName;
                    }
                    if (dbCustomer.HomePhone != ho.HomePhone)
                    {
                        dbCustomer.HomePhone = ho.HomePhone;
                    }
                    if (dbCustomer.MobilePhone != ho.MobilePhone)
                    {
                        dbCustomer.MobilePhone = ho.MobilePhone;
                    }
                    if (dbCustomer.OwnerOrder != ho.OwnerOrder)
                    {
                        dbCustomer.OwnerOrder = ho.OwnerOrder;
                    }
                    if (dbCustomer.PercentOwnership != ho.PercentOwnership)
                    {
                        dbCustomer.PercentOwnership = ho.PercentOwnership;
                    }
                    if (dbCustomer.Acknowledge != ho.Acknowledge)
                    {
                        dbCustomer.Acknowledge = ho.Acknowledge;
                    }
                    if (dbCustomer.Address.City != ho.Address?.City)
                    {
                        dbCustomer.Address.City = ho.Address.City;
                    }
                    if (dbCustomer.Address.PostalCode != ho.Address?.PostalCode)
                    {
                        dbCustomer.Address.PostalCode = ho.Address.PostalCode;
                    }
                    if (dbCustomer.Address.State != ho.Address?.State)
                    {
                        dbCustomer.Address.State = ho.Address.State;
                    }
                    if (dbCustomer.Address.Street != ho.Address?.Street)
                    {
                        dbCustomer.Address.Street = ho.Address.Street;
                    }
                    if (dbCustomer.Address.Unit != ho.Address?.Unit)
                    {
                        dbCustomer.Address.Unit = ho.Address.Unit;
                    }
                }                
            });
        }

        #endregion
    }
}
