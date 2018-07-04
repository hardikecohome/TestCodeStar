using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DealnetPortal.Api.Core.ApiClient;
using DealnetPortal.Api.Integration.ServiceAgents;
using DealnetPortal.Aspire.Integration.Models;
using DealnetPortal.Aspire.Integration.ServiceAgents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Aspire
{
    [TestClass]
    public class XmlRequestsTest
    {
        private IHttpApiClient _httpApiClient;
        private IAspireServiceAgent _aspireServiceAgent;

        [TestInitialize]
        public void Intialize()
        {
            _httpApiClient = new HttpApiClient(ConfigurationManager.AppSettings["AspireApiUrl"]);
            _aspireServiceAgent = new AspireServiceAgent(_httpApiClient);
        }


        [Ignore]
        [TestMethod]
        public void TestPrepareRequestForCustomerUpload()
        {
            DealUploadRequest request = new DealUploadRequest();

            request.Header = new RequestHeader()
            {
                From = new From()
                {
                    AccountNumber = "Admin",
                    Password = "b"
                }
            };

            request.Payload = new Payload()
            {
                Lease = new Lease()
                {   
                    Application = new Application(),
                    Accounts = new List<Account>()
                    {
                        new Account()
                        {
                            Role = "CUST",
                            EmailAddress = "custname@domain.com",
                            IsPrimary = true,
                            Personal = new Personal()
                            {
                                Firstname = "Customer",
                                Lastname = "Name",
                                Dob = DateTime.Today.ToString("d", CultureInfo.CreateSpecificCulture("en-US"))
                            }
                        }
                    }
                }
            };

            var x = new XmlSerializer(request.GetType());
            var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
            FileStream fs = new FileStream("testResponse.xml", FileMode.Create);
            var writer = XmlWriter.Create(fs, settings);
            x.Serialize(writer, request);
            writer.Flush();
        }

        [TestMethod]
        public void TestAspireCustomerUpdate()
        {
            CustomerRequest request = new CustomerRequest();

            request.Header = new RequestHeader()
            {
                UserId = ConfigurationManager.AppSettings["AspireUser"],
                Password = ConfigurationManager.AppSettings["AspirePassword"]
            };

            var response = _aspireServiceAgent.CustomerUploadSubmission(request).GetAwaiter().GetResult();
        }
    }
}
