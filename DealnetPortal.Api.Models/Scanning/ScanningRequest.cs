using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Scanning
{
    /// <summary>
    /// Request with image for recognize
    /// </summary>
    public class ScanningRequest
    {
        /// <summary>
        /// Id of operation (not used)
        /// </summary>
        public string OperationId { get; set; }
        /// <summary>
        /// Image raw data
        /// </summary>
        public byte[] ImageForReadRaw { get; set; }
    }
}
