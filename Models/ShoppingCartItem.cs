
using System;
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class ShoppingCartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Khách hàng sở hữu sản phẩm trong giỏ
        public Customer Customer { get; set; } = null!;

        // Sản phẩm được thêm vào giỏ
        public Product Product { get; set; } = null!;
    }
}
