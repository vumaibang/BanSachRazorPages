
using System;
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(255)]
        public string? ShippingAddress { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(50)]
public string PaymentMethod { get; set; } = "COD";

        // Khách hàng đặt đơn
        public Customer Customer { get; set; } = null!;

        // Danh sách chi tiết của đơn hàng
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
