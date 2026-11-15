
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // Đơn hàng chứa chi tiết này
        public Order Order { get; set; } = null!;

        // Sản phẩm được mua
        public Product Product { get; set; } = null!;
    }
}
