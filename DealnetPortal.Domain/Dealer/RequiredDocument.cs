using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain.Dealer
{
    public class RequiredDocument
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DocumentTypeId { get; set; }
        [ForeignKey("DocumentTypeId")]
        public DocumentType DocumentType { get; set; }

        public string DocumentName { get; set; }
        public byte[] DocumentBytes { get; set; }

        public DateTime CreationDate { get; set; }
        /// <summary>
        /// Uploaded to Aspire
        /// </summary>
        public bool Uploaded { get; set; }
        public DateTime? UploadDate { get; set; }

        public DocumentStatus? Status { get; set; }

        public int DealerInfoId { get; set; }
        [ForeignKey(nameof(DealerInfoId))]
        [Required]
        public DealerInfo DealerInfo { get; set; }
    }
}
