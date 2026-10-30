namespace lucky_draw.Models
{
    // Bảng tạm nhận dữ liệu từ file import, mỗi cột ứng với 1 cột trong file Excel/CSV (xem ExcelImportService.ColumnNames)
    public class Temptable
    {
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? AccountNo { get; set; }
        public string? FullName { get; set; }
        public string? IdNumber { get; set; }
        public string? Address { get; set; }
        public string? ClientNo { get; set; }
        public string? CardNo { get; set; }
    }
}
