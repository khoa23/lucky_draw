using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    // Bảng lưu trữ kết quả đã xóa, không có khóa chính (chỉ ghi bằng SQL trong ReportController)
    [Table("CustomerRewardHistory")]
    public class CustomerRewardHistory
    {
        public int? Id { get; set; }
        public int ProgramId { get; set; }
        public int RewardId { get; set; }
        public int? CustomerId { get; set; }
        public string? ResultCode { get; set; }
        public DateTime? TimeStamp { get; set; }
        public int? NumberOfCustomer { get; set; }
    }
}
