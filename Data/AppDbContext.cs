
using BanSachRazorPages.Models;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Khai báo 7 bảng dữ liệu
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ShoppingCartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình tên bảng
            modelBuilder.Entity<Account>().ToTable("Accounts");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
            modelBuilder.Entity<ShoppingCartItem>().ToTable("CartItems");

            // 1. ACCOUNT
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .IsRequired();
            });

            // 2. CUSTOMER
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);

                entity.Property(e => e.FullName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(255);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
            });

            // 3. CATEGORY
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);

                entity.Property(e => e.CategoryName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // 4. PRODUCT
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);

                entity.Property(e => e.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Author).HasMaxLength(100);
                entity.Property(e => e.Publisher).HasMaxLength(100);
                entity.Property(e => e.ISBN).HasMaxLength(50);

                entity.Property(e => e.Price)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(255);
            });

            // 5. ORDER
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);

                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.ShippingAddress).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Note).HasMaxLength(500);
            });

            // 6. ORDER DETAIL
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(e => e.OrderDetailId);

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2);
            });

            // 7. SHOPPING CART ITEM
            modelBuilder.Entity<ShoppingCartItem>(entity =>
            {
                entity.HasKey(e => e.CartItemId);

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Một khách hàng không có sản phẩm trùng nhau trong giỏ
                entity.HasIndex(e => new { e.CustomerId, e.ProductId })
                    .IsUnique();

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 8. ACCOUNT - CUSTOMER (1 - 1)
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Account)
                .WithOne(a => a.Customer)
                .HasForeignKey<Customer>(c => c.AccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // 9. CUSTOMER - ORDER (1 - N)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 10. CATEGORY - PRODUCT (1 - N)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 11. ORDER - ORDER DETAIL (1 - N)
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 12. PRODUCT - ORDER DETAIL (1 - N)
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
