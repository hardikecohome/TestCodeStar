using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class CustomerCreditReport
    {        
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public int Beacon { get; set; }
        public DateTime CreditLastUpdateTime { get; set; }

        [ForeignKey(nameof(Id))]
        [Required]
        public Customer Customer { get; set; }
    }
}
