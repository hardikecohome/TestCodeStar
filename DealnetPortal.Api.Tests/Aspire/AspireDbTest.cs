using System;
using System.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.DataAccess;
using DealnetPortal.Utilities.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DealnetPortal.Api.Tests.Aspire
{
    [Ignore]
    [TestClass]
    public class AspireDbTest
    {
        private IAspireStorageReader _aspireStorageService;
        private Mock<IQueriesStorage> _queriesStorageMock;
        private IDatabaseService _databaseService;
        private Mock<ILoggingService> _loggingServiceMock;

        [TestInitialize]
        public void Intialize()
        {
            _loggingServiceMock = new Mock<ILoggingService>();
            _databaseService = new MsSqlDatabaseService(ConfigurationManager.ConnectionStrings["AspireConnection"]?.ConnectionString);
            _queriesStorageMock = new Mock<IQueriesStorage>();
            _aspireStorageService = new AspireStorageReader(_databaseService, _queriesStorageMock.Object, _loggingServiceMock.Object);
        }

        [TestMethod]
        public void TestGetGenericFieldValues()
        {
            var list = _aspireStorageService.GetGenericFieldValues();
            Assert.IsNotNull(list);
            Assert.IsTrue(list.Any());
        }

        [TestMethod]
        public void TestGetSubDealers()
        {
            var list = _aspireStorageService.GetSubDealersList("Smart Home");
            
            Assert.IsNotNull(list);
            Assert.IsTrue(list.Any());
        }


        [TestMethod]
        public void TestGetDealerDeals()
        {
            var list = _aspireStorageService.GetDealerDeals("Eco Smart");

            Assert.IsNotNull(list);
            Assert.IsTrue(list.Any());
        }

        [TestMethod]
        public void TestGetDealById()
        {
            int transactionId = 19671;
            //var deal = _aspireStorageService..GetDealById(transactionId);

            //Assert.IsNotNull(deal);
            //Assert.AreEqual(transactionId, int.Parse(deal.Details.TransactionId));
        }

        [TestMethod]
        public void TestGetCustomerById()
        {
            var customerId = "7954";
            var customer = _aspireStorageService.GetCustomerById(customerId);

            Assert.IsNotNull(customer);
            Assert.IsTrue(customer.EntityId.Contains(customerId));
        }

        [TestMethod]
        public void TestFindCustomer()
        {
            var postalCode = "M1H2Y4";
            var firstName = "aaaa";
            var lastName = "ABBAK";
            var dob = DateTime.Parse("1949-08-06 00:00:00.000");
            var customer = _aspireStorageService.FindCustomer(firstName, lastName, dob, postalCode);

            Assert.IsNotNull(customer);            
        }
    }
}
