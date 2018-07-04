using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Models.Aspire
{
    /// <summary>
    /// Request for DealUploadSubmission Aspire API method
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "LeaseXML")]
    public class DealUploadRequest
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("payloadID")]
        public string PayloadId { get; set; }

        [XmlAttribute("timestamp")]
        public string Timestamp { get; set; }

        public RequestHeader Header { set; get; }
        public Payload Payload { set; get; }
    }
}
