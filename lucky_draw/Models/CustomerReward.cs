using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("CustomerReward")]
    public class CustomerReward
    {
        public int Id { get; set; }
        public int ProgramId { get; set; }
        public int RewardId { get; set; }
        public int? CustomerId { get; set; }
        public string? ResultCode { get; set; }
        public DateTime? TimeStamp { get; set; }
        public int? NumberOfCustomer { get; set; }

        public LuckyProgram Program { get; set; }
        public Reward Reward { get; set; }
        public Customer? Customer { get; set; }
    }
}
