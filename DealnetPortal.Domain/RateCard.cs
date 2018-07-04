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
    public class RateCard
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public RateCardType CardType { get; set; }

        public double LoanValueFrom { get; set; }

        public double LoanValueTo { get; set; }

        public double CustomerRate { get; set; }

        public double DealerCost { get; set; }

        public double AdminFee { get; set; }

        public decimal LoanTerm { get; set; }

        public decimal AmortizationTerm { get; set; }

        public decimal DeferralPeriod { get; set; }
        
        public DateTime? ValidFrom { get; set; }
        
        public DateTime? ValidTo { get; set; }

        public bool IsPromo { get; set; }

        public int TierId { get; set; }
        [ForeignKey("TierId")]
        public virtual Tier Tier { get; set; }

        public int? CustomerRiskGroupId { get; set; }
        [ForeignKey("CustomerRiskGroupId")]
        public virtual CustomerRiskGroup CustomerRiskGroup { get; set; }
    }
}
