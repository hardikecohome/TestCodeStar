using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionCustomerXML")]
    public class DecisionCustomerResponse : DealUploadResponse
    {
    }
}
