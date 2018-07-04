using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Models.Profile
{
    public class DealerEquipmentDTO
    {
        public int ProfileId { get; set; }

        public EquipmentTypeDTO Equipment { get; set; }
    }
}