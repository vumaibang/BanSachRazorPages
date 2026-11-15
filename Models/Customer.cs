
using System;
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        public int? AccountId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Tài khoản liên kết với khách hàng
        public Account? Account { get; set; }

        // Danh sách đơn hàng của khách hàng
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        // Danh sách sản phẩm trong giỏ hàng
public ICollection<ShoppingCartItem> CartItems { get; set; }
    = new List<ShoppingCartItem>();
        public bool IsActive { get; set; } = true;
    }
}
