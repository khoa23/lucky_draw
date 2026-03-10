namespace lucky_draw.Models
{
    public class Temptable
    {
        public int Id { get; set; } // Thêm cột Id tự tăng nếu muốn EF quản lý khoá chính
        public string? BRCD { get; set; }
        public string? LCLBRNM { get; set; }
        public string? FTRSERIES { get; set; }
        public string? LSTSERIES { get; set; }
        public string? IDXACNO { get; set; }
        public string? NM { get; set; }
        public string? IDNO { get; set; }
        public string? DIACHI { get; set; }
        public string? CUSTSEQ { get; set; }
        public string? CARDNO { get; set; }
    }
}
