using lucky_draw.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System; // Added for DateTime
using System.ComponentModel.DataAnnotations.Schema; // Added for [Table] attribute

namespace lucky_draw.Data
{
    public class LuckyDrawDbContext : DbContext
    {
        public LuckyDrawDbContext(DbContextOptions<LuckyDrawDbContext> options) : base(options) { }

        //public DbSet<Program> Programs { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<RewardType> RewardTypes { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerInfo> CustomerInfos { get; set; }
        public DbSet<CustomerReward> CustomerReward { get; set; }
        public DbSet<Temptable> Temptable { get; set; }
        public DbSet<LuckyProgram> Programs { get; set; }
        public DbSet<CustomerRewardHistory> CustomerRewardHistory { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerRewardHistory>().HasNoKey();
        }
    }
}
