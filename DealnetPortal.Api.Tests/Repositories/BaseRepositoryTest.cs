using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Repositories
{
    [TestClass]
    public class BaseRepositoryTest
    {
        protected IDatabaseFactory _databaseFactory;
        protected IUnitOfWork _unitOfWork;        
        protected ApplicationUser _user;        

        //[TestInitialize]
        protected void InitializeTestDatabase()
        {
            Database.SetInitializer(
                new DropCreateDatabaseAlways<ApplicationDbContext>());

            _databaseFactory = new DatabaseFactory();
            _unitOfWork = new UnitOfWork(_databaseFactory);            

            var context = _databaseFactory.Get();
            context.Database.Initialize(true);

            InitTestData();
        }

        protected void InitTestData()
        {
            _user = _databaseFactory.Get().Users.FirstOrDefault();
            if (_user == null)
            {
                _user = CreateTestUser();
                _databaseFactory.Get().Users.Add(_user);
                _unitOfWork.Save();
            }
            SetDocumentTypes();
        }

        protected void SetDocumentTypes()
        {
            var documentTypes = new[]
            {
                new DocumentType() {Description = "Signed contract", Prefix = "SC_"},
                new DocumentType() {Description = "Signed Installation certificate", Prefix = "SIC_"},
                new DocumentType() {Description = "Invoice", Prefix = "INV_"},
                new DocumentType() {Description = "Copy of Void Personal Cheque", Prefix = "VPC_"},
                new DocumentType() {Description = "Extended Warranty Form", Prefix = "EWF_"},
                new DocumentType() {Description = "Third party verification call", Prefix = "TPV_"},
                new DocumentType() {Description = "Other", Prefix = ""},
            };
            _databaseFactory.Get().DocumentTypes.AddRange(documentTypes);
            _unitOfWork.Save();
        }

        protected ApplicationUser CreateTestUser()
        {
            var user = new ApplicationUser()
            {
                Email = "user@user.ru",
                UserName = "user@user.ru",
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
            };
            return user;
        }
    }
}
