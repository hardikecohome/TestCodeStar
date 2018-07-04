using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class Language
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(2)]
        public string Code { get; set; }
        [MaxLength(20)]
        public string Name { get; set; }
    }
}
