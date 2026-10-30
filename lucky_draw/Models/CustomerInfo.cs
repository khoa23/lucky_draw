using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("CustomerInfo")]
    public class CustomerInfo
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string? ClientNo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? AccountNo { get; set; }
        public string? IdNo { get; set; }
        public string? AddNo { get; set; }
        public string? TelNo { get; set; }
        public string? Kind { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Time { get; set; }
        public bool? Status { get; set; }
    }
}
