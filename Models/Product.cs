
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Author { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        public int? PublishYear { get; set; }

        [StringLength(50)]
        public string? ISBN { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Danh mục chứa sản phẩm
        public Category Category { get; set; } = null!;

        // Danh sách chi tiết đơn hàng chứa sản phẩm này
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        // Danh sách giỏ hàng chứa sản phẩm này
public ICollection<ShoppingCartItem> CartItems { get; set; }
    = new List<ShoppingCartItem>();
    }
}
