using System;
using System.IO;
using DealnetPortal.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.Api.Tests.Repositories
{
    [TestClass]
    public class SettingsRepositoryTest : BaseRepositoryTest
    {
        protected ISettingsRepository _settingsRepository;
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
            InitUserSettings();
            _settingsRepository = new SettingsRepository(_databaseFactory);
        }

        private void InitUserSettings()
        {
            var setting1 = new SettingItem() {Name = "Setting1", SettingType = SettingType.StringValue};
            var setting2 = new SettingItem() { Name = "Setting2", SettingType = SettingType.StringValue };
            var keysList = new List<SettingItem> { setting1, setting2 };
            _databaseFactory.Get().SettingItems.AddRange(keysList);
            _unitOfWork.Save();

            var userSettings = _databaseFactory.Get().UserSettings.Add(new UserSettings()
            {
                SettingValues = new List<SettingValue>()
                {
                    new SettingValue() { Item = setting1, StringValue = "Value1"},
                    new SettingValue() { Item = setting2, StringValue = "Value2"},
                }
            });
            _user.Settings = userSettings;
            _unitOfWork.Save();
        }

        [TestMethod]
        public void TestGetUserSettings()
        {
            var userSettings = _settingsRepository.GetUserSettings(_user.Id);
            Assert.IsNotNull(userSettings);
            Assert.IsNotNull(userSettings.SettingValues);
            Assert.AreEqual(userSettings.SettingValues.Count, 2);
        }
    }
}
