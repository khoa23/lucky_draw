using System.ComponentModel.DataAnnotations.Schema;

namespace lucky_draw.Models
{
    [Table("Customers")]
    public class Customer
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; }
        public string Name { get; set; }
        public byte? Status { get; set; }
    }
}
