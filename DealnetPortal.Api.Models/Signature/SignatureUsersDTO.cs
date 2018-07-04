using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Signature
{
    public class SignatureUsersDTO
    {
        public int ContractId { get; set; }
        public IList<SignatureUser> Users { get; set; }
    }
}
