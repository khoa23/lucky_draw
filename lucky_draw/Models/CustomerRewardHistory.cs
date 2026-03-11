using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace lucky_draw.Models
{
    [Table("CustomerRewardHistory")]
    public class CustomerRewardHistory
    {
        [Key]
        public int ArchiveId { get; set; }
        public int? Id { get; set; }
        public int ProgramId { get; set; }
        public int RewardId { get; set; }
        public int? CustomerId { get; set; }
        public string? ResultCode { get; set; }
        public DateTime? TimeStamp { get; set; }
        public int? NumberOfCustomer { get; set; }

        public virtual LuckyProgram? Program { get; set; }
        public virtual Reward? Reward { get; set; }
        public virtual Customer? Customer { get; set; }
    }
}
