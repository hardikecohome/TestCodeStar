using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Models.Aspire
{
    /// <summary>
    /// Request for CustomerUploadSubmission Aspire API method
    /// </summary>
    [Serializable]
    [XmlRoot(ElementName = "CustomerXML")]
    public class CustomerRequest
    {
        public RequestHeader Header { set; get; }

        public Payload Payload { set; get; }
    }
}
