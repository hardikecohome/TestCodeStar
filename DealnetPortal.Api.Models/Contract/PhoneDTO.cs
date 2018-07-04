using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class PhoneDTO
    {
        public int Id { get; set; }
        public PhoneType PhoneType { get; set; }

        public string PhoneNum { get; set; }

        public int CustomerId { get; set; }
    }
}
