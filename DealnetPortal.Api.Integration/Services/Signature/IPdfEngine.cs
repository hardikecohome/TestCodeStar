using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Signature;

namespace DealnetPortal.Api.Integration.Services.Signature
{
    public interface IPdfEngine
    {
        Tuple<Stream, IList<Alert>> InsertFormFields(Stream inFormStream, IList<FormField> formFields);

        Tuple<IList<FormField>, IList<Alert>> GetFormfFields(Stream inFormStream);
    }
}
