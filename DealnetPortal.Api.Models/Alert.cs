using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models
{
    /// <summary>
    /// Provide client with information about server-method call status
    /// </summary> 
    public class Alert
    {    
        public Alert()
        {
            Type = AlertType.Information;
            Header = string.Empty;
            Message = string.Empty;
            Code = 0;
        }
        /// <summary>
        /// Alert integer code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Type of alert - info / warning / error
        /// </summary>
        public AlertType Type { get; set; }

        /// <summary>
        /// Alert header
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Alert details
        /// </summary>        
        public string Message { get; set; }
    }
}
