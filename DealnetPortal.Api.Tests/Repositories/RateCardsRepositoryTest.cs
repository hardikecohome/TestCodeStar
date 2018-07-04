using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Repositories
{
    [TestClass]
    public class RateCardsRepositoryTest : BaseRepositoryTest
    {
        protected IRateCardsRepository _rateCardsRepository;
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
            InitTiers();
            _rateCardsRepository = new RateCardsRepository(_databaseFactory);
        }

        [TestMethod]
        public void TestGetTierByDealerId()
        {
            var id = _user.Id;
            var tier = _rateCardsRepository.GetTierByDealerId(id, 0, DateTime.Now, null);
            Assert.IsNotNull(tier);
            Assert.IsTrue(tier.RateCards.Any());
        }

        private void InitTiers()
        {
            var tier = new Tier(){Name = "Tier 1"};
            _databaseFactory.Get().Tiers.Add(tier);
            var rateCard = new RateCard() {};
            tier.RateCards.Add(rateCard);
            _user.Tier = tier;
            _unitOfWork.Save();                        
        }
    }
}
