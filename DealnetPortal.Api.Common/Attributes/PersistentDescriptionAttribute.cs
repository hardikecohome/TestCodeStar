using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Attributes
{
    public class PersistentDescriptionAttribute : DescriptionAttribute
    {
        public PersistentDescriptionAttribute(string description): base(description) {}
    }
}
