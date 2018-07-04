using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Enumeration
{
    public enum SignatureStatus
    {
        NotInitiated = 0,
        Created = 1,
        Sent = 2,
        Delivered =3,
        Signed = 4,
        Completed = 5,
        Declined = 6,
        Deleted = 7
    }
}
