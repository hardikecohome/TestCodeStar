using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionXML")]
    public class CreditCheckResponse : DealUploadResponse
    {
    }
}
