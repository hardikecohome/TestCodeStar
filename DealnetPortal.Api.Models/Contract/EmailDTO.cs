using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class EmailDTO
    {
        public int Id { get; set; }
        public EmailType EmailType { get; set; }

        public string EmailAddress { get; set; }

        public int CustomerId { get; set; }
    }
}
