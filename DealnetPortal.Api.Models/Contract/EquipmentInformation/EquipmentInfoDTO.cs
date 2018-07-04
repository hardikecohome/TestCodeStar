using System;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Api.Models.Contract.EquipmentInformation
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class EquipmentInfoDTO
    {
        public int Id { get; set; }
        public AgreementType AgreementType { get; set; }
        public List<NewEquipmentDTO> NewEquipment { get; set; }
        public List<ExistingEquipmentDTO> ExistingEquipment { get; set; }
        public List<InstallationPackageDTO> InstallationPackages { get; set; }
        public decimal? TotalMonthlyPayment { get; set; }

        public int? RequestedTerm { get; set; }

        public int? LoanTerm { get; set; }

        public int? AmortizationTerm { get; set; }

        public DeferralType DeferralType { get; set; }

        public decimal? CustomerRate { get; set; }

        public decimal? AdminFee { get; set; }

        public decimal? DownPayment { get; set; }

        public decimal? ValueOfDeal { get; set; }

        public string SalesRep { get; set; }

        public DateTime? PreferredStartDate { get; set; }
        public string Notes { get; set; }

        public DateTime? EstimatedInstallationDate { get; set; }

        public DateTime? InstallationDate { get; set; }

        public string InstallerFirstName { get; set; }

        public string InstallerLastName { get; set; }

        public int ContractId { get; set; }

        public int? RateCardId { get; set; }

        public int? CustomerRiskGroupId { get; set; }

        public bool? IsFeePaidByCutomer { get; set; }

        public bool? IsCustomRateCard { get; set; }

        public bool? IsClarityProgram { get; set; }

        public decimal? DealerCost { get; set; }
        // Customer (borrower) has existing agreements
        public bool? HasExistingAgreements { get; set; }

        public AnnualEscalationType? RentalProgramType { get; set; }

        public decimal? RateReduction { get; set; }

        public decimal? RateReductionCost { get; set; }
        public int? RateReductionCardId { get; set; }
    }
}
