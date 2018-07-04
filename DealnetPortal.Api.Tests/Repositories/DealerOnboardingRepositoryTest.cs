using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DealnetPortal.Aspire.Integration.Models;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Domain.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Address = DealnetPortal.Domain.Address;

namespace DealnetPortal.Api.Tests.Repositories
{
    [TestClass]
    public class DealerOnboardingRepositoryTest : BaseRepositoryTest
    {
        protected IDealerOnboardingRepository _dealerOnboardingRepository;
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            AppDomain.CurrentDomain.SetData("DataDirectory",
                Path.Combine(context.TestDeploymentDir, string.Empty));
        }

        [TestInitialize]
        public void Initialize()
        {
            InitializeTestDatabase();
            _dealerOnboardingRepository = new DealerOnboardingRepository(_databaseFactory);
        }

        [TestMethod]
        public void AddNewDealerInfoTest()
        {            
            var dealerInfo = GetTestDealerInfo();            
            var dInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(dealerInfo);
            _unitOfWork.Save();

            Assert.IsNotNull(dInfo);
        }

        [TestMethod]
        public void UpdateDealerInfoTest()
        {
            var dealerInfo = GetTestDealerInfo();
            dealerInfo.CompanyInfo.Provinces.Add(new CompanyProvince()
            {
                Province = "ON"
            });
            dealerInfo.CompanyInfo.Provinces.Add(new CompanyProvince()
            {
                Province = "AB"
            });
            var dInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(dealerInfo);
            _unitOfWork.Save();
            Assert.IsNotNull(dInfo);

            var updateInfo = GetTestDealerInfo();
            updateInfo.Id = dInfo.Id;
            updateInfo.CompanyInfo.Id = dInfo.CompanyInfo.Id;
            updateInfo.CompanyInfo.FullLegalName = "Updated FullName";
            updateInfo.CompanyInfo.Provinces = new List<CompanyProvince>() {
                new CompanyProvince()
                {
                    Province = "ON"
                }, 
                new CompanyProvince()
                {
                    Province = "SE"
                }};
            dInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(updateInfo);
            _unitOfWork.Save();
            Assert.IsNotNull(dInfo);
        }

        [TestMethod]
        public void AddAndUpdateRequiredDocumentTest()
        {
            var newDocument = new RequiredDocument()
            {
                DocumentName = "Test1",
                DocumentTypeId = 1
            };
            var dbDoc = _dealerOnboardingRepository.AddDocumentToDealer(0, newDocument);
            _unitOfWork.Save();
            Assert.IsNotNull(dbDoc);

            var updateDocument = new RequiredDocument()
            {
                Id = dbDoc.Id,
                DocumentTypeId = dbDoc.DocumentTypeId,
                DealerInfoId = dbDoc.DealerInfoId,
                DocumentName = "Test2"
            };
            dbDoc = _dealerOnboardingRepository.AddDocumentToDealer(dbDoc.Id, updateDocument);
            _unitOfWork.Save();
            Assert.IsNotNull(dbDoc);
        }

        [TestMethod]
        public void DeleteDealerInfoTest()
        {
            var dealerInfo = GetTestDealerInfo();
            dealerInfo.CompanyInfo.Provinces.Add(new CompanyProvince()
            {
                Province = "ON"
            });
            dealerInfo.CompanyInfo.Provinces.Add(new CompanyProvince()
            {
                Province = "AB"
            });
            dealerInfo.ProductInfo = new ProductInfo()
            {
                PrimaryBrand = "Brand",
                Brands = new List<ManufacturerBrand>() {new ManufacturerBrand() {Brand = "Brand 2"}},
            };
            var dInfo = _dealerOnboardingRepository.AddOrUpdateDealerInfo(dealerInfo);
            _unitOfWork.Save();
            Assert.IsNotNull(dInfo);

            var newDocument = new RequiredDocument()
            {
                DocumentName = "Test1",
                DocumentTypeId = 1
            };
            var dbDoc = _dealerOnboardingRepository.AddDocumentToDealer(dInfo.Id, newDocument);
            _unitOfWork.Save();
            Assert.IsNotNull(dbDoc);

            var isDeleted = _dealerOnboardingRepository.DeleteDealerInfo(dInfo.Id);
            _unitOfWork.Save();
            Assert.IsTrue(isDeleted);
        }

        private DealerInfo GetTestDealerInfo()
        {
            var owners = new List<OwnerInfo>();
            owners.Add(new OwnerInfo()
            {
                FirstName = "First1",
                LastName = "Last1",
                Address = new Address()
                {
                    Street = "Street1"
                },
                PercentOwnership = 40
            });
            owners.Add(new OwnerInfo()
            {
                FirstName = "First2",
                LastName = "Last2",
                Address = new Address()
                {
                    Street = "Street2"
                },
                PercentOwnership = 20
            });
            var dealerInfo = new DealerInfo()
            {
                ParentSalesRep = _user,
                CompanyInfo = new CompanyInfo()
                {
                    FullLegalName = "FullName",

                },
                Owners = owners
            };
            return dealerInfo;
        }
    }
}
