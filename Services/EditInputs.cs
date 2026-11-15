using System.ComponentModel.DataAnnotations;
using BanSachRazorPages.Models;

namespace BanSachRazorPages.Services;

public class ProductInput
{
    [Required(ErrorMessage = "Nhập tên sách."), StringLength(200)] public string ProductName { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục.")] public int CategoryId { get; set; }
    [StringLength(100)] public string? Author { get; set; }
    [StringLength(100)] public string? Publisher { get; set; }
    [Range(1, 9999)] public int? PublishYear { get; set; }
    [StringLength(50)] public string? ISBN { get; set; }
    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "Giá bán phải lớn hơn 0.")] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int StockQuantity { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Product ToEntity(int id, string? imageUrl) => new() { ProductId = id, ProductName = ProductName, CategoryId = CategoryId, Author = Author, Publisher = Publisher, PublishYear = PublishYear, ISBN = ISBN, Price = Price, StockQuantity = StockQuantity, Description = Description, IsActive = IsActive, ImageUrl = imageUrl };
    public static ProductInput From(Product p) => new() { ProductName = p.ProductName, CategoryId = p.CategoryId, Author = p.Author, Publisher = p.Publisher, PublishYear = p.PublishYear, ISBN = p.ISBN, Price = p.Price, StockQuantity = p.StockQuantity, Description = p.Description, IsActive = p.IsActive };
}
public class CategoryInput
{
    [Required(ErrorMessage = "Nhập tên danh mục."), StringLength(100)] public string CategoryName { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Category ToEntity(int id) => new() { CategoryId = id, CategoryName = CategoryName, Description = Description, IsActive = IsActive };
    public static CategoryInput From(Category c) => new() { CategoryName = c.CategoryName, Description = c.Description, IsActive = c.IsActive };
}
public class CustomerInput
{
    [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(20)] public string? Phone { get; set; }
    [EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(255)] public string? Email { get; set; }
    [StringLength(255)] public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public Customer ToEntity(int id) => new() { CustomerId = id, FullName = FullName, Phone = Phone, Email = Email, Address = Address, IsActive = IsActive };
    public static CustomerInput From(Customer c) => new() { FullName = c.FullName, Phone = c.Phone, Email = c.Email, Address = c.Address, IsActive = c.IsActive };
}
