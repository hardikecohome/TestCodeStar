
using System.ComponentModel;

namespace DealnetPortal.Api.Common.Enumeration
{
    public enum DocumentTemplateType
    {
        [Description("Signed contract")]
        SignedContract = 1,
        [Description("Signed Installation certificate")]
        SignedInstallationCertificate = 2,
        [Description("Invoice")]
        Invoice = 3,
        [Description("Copy of Void Personal Cheque")]
        VoidPersonalCheque = 4,
        [Description("Extended Warranty Form")]
        ExtendedWarrantyForm = 5,
        [Description("Third party verification call")]
        VerificationCall = 6,
        [Description("Other")]
        Other = 7,
        [Description("Insurence")]
        Insurence = 8,
        [Description("Void Cheque or Bank PAP Authorization Letter")]
        ChequeBankPAP = 9,
        [Description("Income verification")]
        IncomeVerification = 10,
        [Description("Credit Application")]
        CreditApplication= 11,
    }
}
