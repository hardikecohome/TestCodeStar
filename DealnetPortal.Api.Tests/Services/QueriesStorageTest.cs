using System;
using DealnetPortal.DataAccess;
using DealnetPortal.Utilities.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Services
{
    [TestClass]
    public class QueriesStorageTest
    {
        private IQueriesStorage _queriesStorage;

        [TestInitialize]
        public void Intialize()
        {
            _queriesStorage = new QueriesFileStorage("Queries");
        }

        [TestMethod]
        public void TestGetQuery()
        {
            var query = _queriesStorage.GetQuery("GetDealerDeals");
            Assert.IsNotNull(query);
        }
    }
}
