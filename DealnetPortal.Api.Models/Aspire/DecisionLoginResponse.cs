using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Models.Aspire
{
    [Serializable]
    [XmlRoot(ElementName = "DecisionLoginXML")]
    public class DecisionLoginResponse
    {
        public ResponseHeader Header { set; get; }
    }
}
