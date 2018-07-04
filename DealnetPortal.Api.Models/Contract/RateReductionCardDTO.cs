using System;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract
{
    public class RateReductionCardDTO
    {
        public int Id { get; set; }

        public int LoanTerm { get; set; }

        public int AmortizationTerm { get; set; }

        public decimal CustomerReduction { get; set; }

        public decimal InterestRateReduction { get; set; }
    }
}