using System;
using System.Collections.Generic;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class RateCardDTO
    {
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

        public CustomerRiskGroupDTO CustomerRiskGroup { get; set; }

        public List<RateReductionCardDTO> RateReductionCards { get; set; }
    }
}