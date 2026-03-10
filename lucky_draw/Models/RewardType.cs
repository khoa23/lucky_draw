using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("RewardTypes")]
    public class RewardType
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? RewardOption { get; set; }

        public ICollection<Reward> Rewards { get; set; }
    }
}
