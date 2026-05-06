

namespace webBackend.Models 
{
    public class CarrierDistrictExclusion
    {
        public int Id { get; set; }
        public int CarrierId { get; set; }
        public int CityId { get; set; } 
        public int? DistrictId { get; set; }

        public virtual Carrier Carrier { get; set; }
        public virtual TblIl City { get; set; }
        public virtual TblIlce District { get; set; }
    }
}