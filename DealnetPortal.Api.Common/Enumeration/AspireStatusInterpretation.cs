using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Enumeration
{
    [Flags]
    public enum AspireStatusInterpretation
    {
        SentToAudit = 1 << 0
    }
}
