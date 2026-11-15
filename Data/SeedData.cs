
using BanSachRazorPages.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanSachRazorPages.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            var passwordHasher = new PasswordHasher<Account>();

            // 1. Tạo tài khoản Admin
            var admin = await context.Accounts
                .FirstOrDefaultAsync(a => a.Username == "admin");

            if (admin == null)
            {
                admin = new Account
                {
                    Username = "admin",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                admin.PasswordHash = passwordHasher.HashPassword(
                    admin, "Admin@123");

                context.Accounts.Add(admin);
                await context.SaveChangesAsync();
            }

            // 2. Tạo danh mục mẫu
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category
                    {
                        CategoryName = "Lập trình",
                        Description = "Sách về lập trình và công nghệ thông tin"
                    },
                    new Category
                    {
                        CategoryName = "Kinh tế",
                        Description = "Sách kinh tế và kinh doanh"
                    },
                    new Category
                    {
                        CategoryName = "Văn học",
                        Description = "Sách văn học trong và ngoài nước"
                    },
                    new Category
                    {
                        CategoryName = "Kỹ năng sống",
                        Description = "Sách phát triển bản thân"
                    }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // 3. Tạo sản phẩm mẫu
            if (!await context.Products.AnyAsync())
            {
                var programming = await context.Categories
                    .FirstAsync(c => c.CategoryName == "Lập trình");

                var economy = await context.Categories
                    .FirstAsync(c => c.CategoryName == "Kinh tế");

                var literature = await context.Categories
                    .FirstAsync(c => c.CategoryName == "Văn học");

                var skills = await context.Categories
                    .FirstAsync(c => c.CategoryName == "Kỹ năng sống");

                var products = new List<Product>
                {
                    new Product
                    {
                        ProductName = "Lập trình C# cơ bản",
                        Author = "Nguyễn Văn A",
                        Publisher = "NXB Giáo Dục",
                        PublishYear = 2024,
                        ISBN = "9780000000001",
                        Price = 120000,
                        StockQuantity = 50,
                        Category = programming
                    },
                    new Product
                    {
                        ProductName = "Lập trình Java",
                        Author = "Trần Văn B",
                        Publisher = "NXB Thông Tin",
                        PublishYear = 2023,
                        ISBN = "9780000000002",
                        Price = 150000,
                        StockQuantity = 40,
                        Category = programming
                    },
                    new Product
                    {
                        ProductName = "Cấu trúc dữ liệu và giải thuật",
                        Author = "Lê Văn C",
                        Publisher = "NXB Giáo Dục",
                        PublishYear = 2024,
                        ISBN = "9780000000003",
                        Price = 180000,
                        StockQuantity = 30,
                        Category = programming
                    },
                    new Product
                    {
                        ProductName = "Kinh tế học cơ bản",
                        Author = "Phạm Văn D",
                        Publisher = "NXB Kinh Tế",
                        PublishYear = 2022,
                        ISBN = "9780000000004",
                        Price = 130000,
                        StockQuantity = 25,
                        Category = economy
                    },
                    new Product
                    {
                        ProductName = "Nhà giả kim",
                        Author = "Paulo Coelho",
                        Publisher = "NXB Văn Học",
                        PublishYear = 2023,
                        ISBN = "9780000000005",
                        Price = 79000,
                        StockQuantity = 60,
                        Category = literature
                    },
                    new Product
                    {
                        ProductName = "Dế mèn phiêu lưu ký",
                        Author = "Tô Hoài",
                        Publisher = "NXB Kim Đồng",
                        PublishYear = 2022,
                        ISBN = "9780000000006",
                        Price = 65000,
                        StockQuantity = 45,
                        Category = literature
                    },
                    new Product
                    {
                        ProductName = "Đắc nhân tâm",
                        Author = "Dale Carnegie",
                        Publisher = "NXB Tổng Hợp",
                        PublishYear = 2023,
                        ISBN = "9780000000007",
                        Price = 86000,
                        StockQuantity = 35,
                        Category = skills
                    },
                    new Product
                    {
                        ProductName = "7 thói quen hiệu quả",
                        Author = "Stephen R. Covey",
                        Publisher = "NXB Trẻ",
                        PublishYear = 2024,
                        ISBN = "9780000000008",
                        Price = 110000,
                        StockQuantity = 20,
                        Category = skills
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            // 4. Tạo khách hàng mẫu
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        FullName = "Nguyễn Văn An",
                        Phone = "0901234567",
                        Email = "an@gmail.com",
                        Address = "TP. Hồ Chí Minh"
                    },
                    new Customer
                    {
                        FullName = "Trần Thị Bình",
                        Phone = "0912345678",
                        Email = "binh@gmail.com",
                        Address = "Hà Nội"
                    },
                    new Customer
                    {
                        FullName = "Lê Văn Cường",
                        Phone = "0923456789",
                        Email = "cuong@gmail.com",
                        Address = "Đà Nẵng"
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // 5. Tạo tài khoản User mẫu
            var customerAn = await context.Customers
                .FirstOrDefaultAsync(c => c.Email == "an@gmail.com");

            if (customerAn != null)
            {
                var user = await context.Accounts
                    .FirstOrDefaultAsync(a => a.Username == "user");

                if (user == null)
                {
                    user = new Account
                    {
                        Username = "user",
                        Role = "Customer",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    user.PasswordHash = passwordHasher.HashPassword(
                        user, "User@123");

                    context.Accounts.Add(user);
                    await context.SaveChangesAsync();
                }

                // Liên kết tài khoản với khách hàng
                customerAn.AccountId = user.AccountId;
                await context.SaveChangesAsync();
            }

            // 6. Tạo đơn hàng mẫu
            if (!await context.Orders.AnyAsync())
            {
                var customer = await context.Customers.FirstAsync();

                var book1 = await context.Products
                    .FirstAsync(p => p.ProductName == "Lập trình C# cơ bản");

                var book2 = await context.Products
                    .FirstAsync(p => p.ProductName == "Nhà giả kim");

                var book3 = await context.Products
                    .FirstAsync(p => p.ProductName == "Đắc nhân tâm");

                var order = new Order
                {
                    Customer = customer,
                    OrderDate = DateTime.Now,
                    Status = "Completed",
                    ShippingAddress = customer.Address,
                    Phone = customer.Phone,
                    Note = "Đơn hàng mẫu",

                    OrderDetails = new List<OrderDetail>
                    {
                        new OrderDetail
                        {
                            Product = book1,
                            Quantity = 2,
                            UnitPrice = book1.Price
                        },
                        new OrderDetail
                        {
                            Product = book2,
                            Quantity = 1,
                            UnitPrice = book2.Price
                        },
                        new OrderDetail
                        {
                            Product = book3,
                            Quantity = 1,
                            UnitPrice = book3.Price
                        }
                    }
                };

                order.TotalAmount = order.OrderDetails.Sum(
                    od => od.Quantity * od.UnitPrice);

                context.Orders.Add(order);
                await context.SaveChangesAsync();
            }
        }
    }
}
