using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("Programs")] // Map tới bảng Programs
    public class LuckyProgram
    {
        public int Id { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateStart { get; set; }
        public string ProgramName { get; set; }
        public string ProgramFilePath { get; set; }
        public byte? NumberSize { get; set; }
        public virtual ICollection<Reward> Rewards { get; set; }
    }

}
