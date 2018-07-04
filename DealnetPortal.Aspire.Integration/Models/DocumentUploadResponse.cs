using System;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionDocumentXML")]
    public class DocumentUploadResponse : DealUploadResponse
    {
        //public ResponseHeader Header { set; get; }
        //public DocumenUploadResponsePayload Payload { get; set; }
    }

    //[Serializable]
    //[System.Xml.Serialization.XmlRoot(ElementName = "Payload")]
    //public class DocumenUploadResponsePayload
    //{
    //    public string TransactionId { get; set; }
    //    public string DocumentName { get; set; }
    //}
}
