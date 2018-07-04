using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class DocumentType
    {
        [Key]
        public int Id { get; set; }
        public string Prefix { get; set; }
        public string Description { get; set; }
        public string DescriptionResource { get; set; }
        public bool IsMandatory { get; set; }
    }
}
