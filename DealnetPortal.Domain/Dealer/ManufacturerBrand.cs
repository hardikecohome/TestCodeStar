using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain.Dealer
{
    public class ManufacturerBrand
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Brand { get; set; }

        public int ProductInfoId { get; set; }
        [ForeignKey(nameof(ProductInfoId))]
        [Required]
        public ProductInfo ProductInfo { get; set; }
    }
}
