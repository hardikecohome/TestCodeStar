using System;
using System.Collections.Generic;
using System.IO;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Repositories
{
    [TestClass]
    public class CustomerFormRepositoryTest : BaseRepositoryTest
    {
        protected ICustomerFormRepository _customerFormRepository;
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
            InitLanguages();
            _customerFormRepository = new CustomerFormRepository(_databaseFactory);
        }

        private void InitLanguages()
        {
            _databaseFactory.Get().Languages.Add(new Language()
            {
                Id = (int) LanguageCode.English,
                Code = "en"
            });
            _databaseFactory.Get().Languages.Add(new Language()
            {
                Id = (int) LanguageCode.French,
                Code = "fr"
            });
            _unitOfWork.Save();
        }

        [TestMethod]
        public void TestEnableLanguages()
        {
            //enable en
            var enabledLangs = new List<DealerLanguage> {new DealerLanguage() {LanguageId = (int) LanguageCode.English}};
            var updatedLink = _customerFormRepository.UpdateCustomerLinkLanguages(enabledLangs, "link", _user.Id);
            _unitOfWork.Save();
            Assert.IsNotNull(updatedLink);
            Assert.AreEqual(updatedLink.EnabledLanguages.Count, 1);
            //disable all lang
            enabledLangs = new List<DealerLanguage>();
            updatedLink = _customerFormRepository.UpdateCustomerLinkLanguages(enabledLangs, "link", _user.Id);
            _unitOfWork.Save();
            Assert.IsNotNull(updatedLink);
            Assert.AreEqual(updatedLink.EnabledLanguages.Count, 0);
        }

        [TestMethod]
        public void TestEnableCustomerLinkServices()
        {
            var newServices = new List<DealerService>
            {
                new DealerService() {LanguageId = (int)LanguageCode.English, Service = "ServiceEn1"},
                new DealerService() {LanguageId = (int)LanguageCode.English, Service = "ServiceEn2"},
                new DealerService() {LanguageId = (int)LanguageCode.French, Service = "ServiceFr1"},
                new DealerService() {LanguageId = (int)LanguageCode.French, Service = "ServiceFr2"},
            };
            var updatedLink = _customerFormRepository.UpdateCustomerLinkServices(newServices, "link", _user.Id);
            _unitOfWork.Save();
            Assert.IsNotNull(updatedLink);
            Assert.AreEqual(updatedLink.Services.Count, 4);
            //leave only 2 services
            newServices = new List<DealerService>
            {
                new DealerService() {LanguageId = (int)LanguageCode.English, Service = "ServiceEn1"},
                new DealerService() {LanguageId = (int)LanguageCode.French, Service = "ServiceFr1"},
            };
            updatedLink = _customerFormRepository.UpdateCustomerLinkServices(newServices, "link", _user.Id);
            _unitOfWork.Save();
            Assert.IsNotNull(updatedLink);
            Assert.AreEqual(updatedLink.Services.Count, 2);
            //clean all services
            newServices = new List<DealerService>();            
            updatedLink = _customerFormRepository.UpdateCustomerLinkServices(newServices, "link", _user.Id);
            _unitOfWork.Save();
            Assert.IsNotNull(updatedLink);
            Assert.AreEqual(updatedLink.Services.Count, 0);
        }
    }
}
