
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Danh sách sản phẩm thuộc danh mục
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
