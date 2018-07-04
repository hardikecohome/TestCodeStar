using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Storage
{
    public class AgreementTemplateDTO
    {
        public int Id { get; set; }

        public string TemplateName { get; set; }

        public AgreementType? AgreementType { get; set; }

        public string State { get; set; }

        public string DealerId { get; set; }

        public string DealerName { get; set; }

        /// <summary>
        /// For Aspire dealers
        /// </summary>
        public string ExternalDealerName { get; set; }

        public byte[] AgreementFormRaw { get; set; }

        public string ExternalTemplateId { get; set; }

        public string EquipmentType { get; set; }

        //public virtual List<string> EquipmentTypes { get; set; }
    }
}
