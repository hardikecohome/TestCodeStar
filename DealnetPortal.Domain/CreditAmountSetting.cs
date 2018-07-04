using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    /// <summary>
    /// Credit amount configuration item
    /// </summary>
    public class CreditAmountSetting
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CreditScoreFrom { get; set; }
        public int CreditScoreTo { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal EscalatedLimit { get; set; }
        public decimal NonEscalatedLimit { get; set; }
    }
}
