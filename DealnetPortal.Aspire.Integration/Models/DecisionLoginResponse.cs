using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionLoginXML")]
    public class DecisionLoginResponse
    {
        public ResponseHeader Header { set; get; }
    }
}
