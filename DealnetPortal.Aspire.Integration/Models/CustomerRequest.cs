using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    /// <summary>
    /// Request for CustomerUploadSubmission Aspire API method
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "CustomerXML")]
    public class CustomerRequest : DealUploadRequest
    {
        //public RequestHeader Header { set; get; }

        //public Payload Payload { set; get; }
    }
}
