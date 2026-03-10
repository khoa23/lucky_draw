using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("CustomerInfo")]
    public class CustomerInfo
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; }
        public string? Cif { get; set; }
        public string Name { get; set; }
        public string? Brcd { get; set; }
        public string? BrcdNm { get; set; }
        public string? Acctno { get; set; }
        public string? IdNo { get; set; }
        public string? AddNo { get; set; }
        public string? TelNo { get; set; }
        public string? Kind { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Time { get; set; }
        public bool? Status { get; set; }
    }
}
