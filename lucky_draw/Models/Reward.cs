using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("Rewards")]
    public class Reward
    {
        public int Id { get; set; }
        public int ProgramId { get; set; }
        public string RewardName { get; set; }
        public int NumberOfReward { get; set; }
        public byte? RewardType { get; set; }
        public string PrefixCode { get; set; }
        public byte? Idd { get; set; }
        public bool? AllowPreviousResultCode { get; set; }
        public bool? AllowMultiResultCode { get; set; }
        public byte? NumberLength { get; set; }
        public int? RelationRewardId { get; set; }
        public byte? MaxWheelTime { get; set; }
        public virtual LuckyProgram Program { get; set; }
        [ForeignKey("RewardType")]
        public virtual RewardType RewardTypeNav { get; set; }
    }

}
