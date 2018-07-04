using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Models.Aspire
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionUploadXML")]
    public class DealUploadResponse
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("payloadID")]
        public string PayloadId { get; set; }

        [XmlAttribute("timestamp")]
        public string Timestamp { get; set; }

        public ResponseHeader Header { set; get; }

        public ResponsePayload Payload { set; get; }
        //public Payload Payload { set; get; }
    }
}
