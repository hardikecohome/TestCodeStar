using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class AgreementTemplateDocument
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        //Name of a template. For automatically Binary data seed, template name should be a name of a PDF-template without extension
        public string TemplateName { get; set; }
        //template can have either binary data, or external (DocuSign) template, or both
        public byte[] TemplateBinary { get; set; }
        //DocuSign template Id
        public string ExternalTemplateId { get; set; }
    }
}
