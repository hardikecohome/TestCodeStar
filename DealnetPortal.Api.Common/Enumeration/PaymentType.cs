using System.ComponentModel.DataAnnotations;

namespace DealnetPortal.Api.Common.Enumeration
{
    public enum PaymentType
    {
        [Display(Name = "ENBRIDGE")]
        Enbridge,
        [Display(Name = "PAD")]
        Pap
    }
}
