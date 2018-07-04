using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "CreditCheckXML")]
    public class CreditCheckRequest : DealUploadRequest
    {
    }
}
