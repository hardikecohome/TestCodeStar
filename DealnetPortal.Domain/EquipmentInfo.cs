using System;
using System.ComponentModel.DataAnnotations;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class EquipmentInfo
    {

        private bool? isCustomRateCard;
        public EquipmentInfo()
        {
            NewEquipment = new HashSet<NewEquipment>();
            ExistingEquipment = new HashSet<ExistingEquipment>();
            InstallationPackages = new HashSet<InstallationPackage>();
        }

        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ForeignKey("Contract")]
        public int Id { get; set; }
        public AgreementType AgreementType { get; set; }
        public ICollection<NewEquipment> NewEquipment { get; set; }
        public ICollection<ExistingEquipment> ExistingEquipment { get; set; }
        public ICollection<InstallationPackage> InstallationPackages { get; set; }
        public decimal? TotalMonthlyPayment { get; set; }

        public int? RequestedTerm { get; set; }

        public int? LoanTerm { get; set; }

        public int? AmortizationTerm { get; set; }

        public DeferralType? DeferralType { get; set; }

        public decimal? CustomerRate { get; set; }
        
        public decimal? AdminFee { get; set; }
        
        public decimal? DownPayment { get; set; }

        public decimal? ValueOfDeal { get; set; }

        public string SalesRep { get; set; }

        public string Notes { get; set; }

        public DateTime? PreferredStartDate { get; set; }
        /// <summary>
        /// Preffered installation date
        /// </summary>
        public DateTime? EstimatedInstallationDate { get; set; }

        /// <summary>
        /// Factical date of equipment installation
        /// </summary>
        public DateTime? InstallationDate { get; set; }

        public string InstallerFirstName { get; set; }

        public string InstallerLastName { get; set; }

        [ForeignKey(nameof(RateCard))]
        public int? RateCardId { get; set; }
        public RateCard RateCard { get; set; }

        [ForeignKey(nameof(CustomerRiskGroup))]
        public int? CustomerRiskGroupId { get; set; }
        public CustomerRiskGroup CustomerRiskGroup { get; set; }

        public bool? IsFeePaidByCutomer { get; set; }

        [NotMapped]
        public bool IsCustomRateCard
        {
            get
            {
                return (RateCard != null && RateCard.CardType == RateCardType.Custom) ||
                       (isCustomRateCard ?? RateCardId == null && AmortizationTerm.HasValue && LoanTerm.HasValue);
            }
            set { isCustomRateCard = value; }
        }

        public bool? IsClarityProgram { get; set; }

        public Contract Contract { get; set; }

        public decimal? DealerCost { get; set; }

        // Customer (borrower) has existing agreements
        public bool? HasExistingAgreements { get; set; }

        public AnnualEscalationType? RentalProgramType { get; set; }

	    public int? RateReductionCardId { get; set; }
        [ForeignKey("RateReductionCardId")]
        public virtual RateReductionCard RateReductionCard { get; set; }

	    public decimal? RateReduction { get; set; }

	    public decimal? RateReductionCost { get; set; }
    }
}
