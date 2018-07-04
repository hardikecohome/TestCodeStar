using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.ESignature
{
    [TestClass]
    public class DocuSignTest
    {
        [TestMethod]
        public void TestNotificationResponse()
        {
            var path = "Files/DocuSignNotification.xml";
            XDocument xDocument = XDocument.Load(path);            
            Assert.IsNotNull(xDocument);
            var xmlns = xDocument?.Root?.Attribute(XName.Get("xmlns"))?.Value ?? "http://www.docusign.net/API/3.0";
            var envelopeStatus = xDocument.Root.Element(XName.Get("EnvelopeStatus", xmlns));
            var status = envelopeStatus.Element(XName.Get("Status", xmlns));
            Assert.AreEqual(status.Value, "Completed");

            var documents =
                xDocument.Root.Element(XName.Get("DocumentPDFs", xmlns));  
            var document = documents.Elements().FirstOrDefault(x => !(x.FirstNode as XElement).Value.Contains("CertificateOfCompletion"));
            Assert.IsNotNull(document);
            var docType = document.Elements().FirstOrDefault(x => x.Name.LocalName == "DocumentType");
            Assert.AreEqual(docType.Value, "CONTENT");
        }
        [TestMethod]
        public void TestRecipientStatuses()
        {
            var path = "Files/DocuSignNotification.xml";
            var docuSignResipientStatuses = new string[] { "Sent", "Delivered", "Signed", "Declined" };
            XDocument xDocument = XDocument.Load(path);
            Assert.IsNotNull(xDocument);
            var xmlns = xDocument?.Root?.Attribute(XName.Get("xmlns"))?.Value ?? "http://www.docusign.net/API/3.0";
            var envelopeStatus = xDocument.Root.Element(XName.Get("EnvelopeStatus", xmlns));
            var recipientStatusesSection = envelopeStatus?.Element(XName.Get("RecipientStatuses", xmlns));
            Assert.IsNotNull(recipientStatusesSection);
            var recipientStatuses = recipientStatusesSection.Descendants(XName.Get("RecipientStatus", xmlns));
            Assert.IsNotNull(recipientStatusesSection);
            Assert.IsTrue(recipientStatuses.Any());
            recipientStatuses.ForEach(rs =>
            {
                var rsStatus = rs.Element(XName.Get("Status", xmlns))?.Value;
                Assert.IsNotNull(rsStatus);
                var rsLastStatusTime = rs.Elements().Where(rse =>
                        docuSignResipientStatuses.Any(ds => rse.Name.LocalName.Contains(ds)))
                    .Select(rse =>
                    {
                        DateTime statusTime;
                        if (!DateTime.TryParse(rse.Value, out statusTime))
                        {
                            statusTime = new DateTime();
                        }
                        return statusTime;
                    }).OrderByDescending(rst => rst).FirstOrDefault();
                var rsName = rs.Element(XName.Get("UserName", xmlns))?.Value;
                Assert.IsNotNull(rsName);
                var rsEmail = rs.Element(XName.Get("Email", xmlns))?.Value;
                Assert.IsNotNull(rsEmail);
            });

        }
    }
}
