using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    public class AgreementTemplate
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public AgreementType? AgreementType { get; set; }

        public AnnualEscalationType? AnnualEscalation { get; set; }

        public int? DocumentTypeId { get; set; }
        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; }

        public string State { get; set; }

        public string EquipmentType { get; set; }

        public string Culture { get; set; }

        public string DealerId { get; set; }
        [ForeignKey("DealerId")]
        public virtual ApplicationUser Dealer { get; set; }

        public string ApplicationId { get; set; }
        [ForeignKey("ApplicationId")]
        public Application Application { get; set; }

        /// <summary>
        /// For Aspire dealers those are not in DB yet
        /// </summary>
        public string ExternalDealerName { get; set; }


        public int? TemplateDocumentId { get; set; }

        [ForeignKey(nameof(TemplateDocumentId))]
        public virtual AgreementTemplateDocument TemplateDocument { get; set; }       
    }
}
