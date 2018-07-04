using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Contract
{
    public class DocumentTypeDTO
    {
        public int Id { get; set; }
        public string Prefix { get; set; }
        public string Description { get; set; }
        public bool IsMandatory { get; set; }
    }
}
