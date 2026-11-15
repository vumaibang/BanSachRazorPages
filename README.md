# Nhà Sách — ASP.NET Core Razor Pages

Giao diện và nghiệp vụ được hoàn thiện dựa trên `BanSachBlazor (2)`, triển khai bằng Razor Pages (`.cshtml` và `PageModel`). Ứng dụng chính sử dụng **BanSachDB có sẵn của Razor**, không chuyển sang database Blazor.

## Chạy ứng dụng

Yêu cầu .NET SDK 10 và MariaDB/MySQL. Cấu hình hiện tại phù hợp MariaDB 10.4.32 trong XAMPP, cổng 3306.

1. Khởi động MySQL/MariaDB trong XAMPP.
2. Kiểm tra `ConnectionStrings:DefaultConnection` trong `appsettings.json`: database `BanSachDB`.
3. Tại thư mục `BanSachRazorPages`, chạy:

```powershell
dotnet restore
dotnet run --launch-profile http
```

Mở trang khách hàng tại <http://localhost:5013/>; đăng nhập quản trị tại <http://localhost:5013/Admin/Login>. Sử dụng các tài khoản đang có trong database. Ứng dụng không đặt lại mật khẩu hoặc tạo lại tài khoản mẫu khi khởi động.

Nếu dùng MySQL thay cho MariaDB, đổi `Database:ServerType` thành `MySql` và `Database:ServerVersion` theo phiên bản máy chủ.

## Cấu trúc

```text
Data/                     AppDbContext và SeedData gốc
Models/                   Các entity gốc
Migrations/               Bốn migration gốc
Pages/
  Admin/                  Trang tổng quan, đăng nhập/đăng xuất
    Products/             CRUD sách và tải ảnh bìa
    Categories/           CRUD danh mục
    Customers/            CRUD và trạng thái khách hàng
    Accounts/             CRUD, vai trò và khóa tài khoản
    Orders/               Xem, lọc và xử lý đơn hàng
  User/                   Trang chủ, đăng ký, đăng nhập/đăng xuất
    Products/             Chi tiết sách
    Cart/                 Giỏ hàng, mua ngay và thanh toán
    Orders/               Đơn của khách và hủy đơn
    Profile/              Thông tin cá nhân và đổi mật khẩu
  Shared/                 Các partial dùng chung, không phải trang điều hướng
Services/                 Nghiệp vụ, xác thực và cấu hình ứng dụng
wwwroot/                  CSS, JavaScript và ảnh
BanSachRazorPages.Tests/   Kiểm thử nghiệp vụ và HTTP
```

Các đường dẫn cũ `/Customer/...`, `/Cart/...`, `/Products/...` được chuyển sang `/User/...`. Trang chủ vẫn có địa chỉ `/`.

## Chức năng

- Khách hàng: tìm theo tên sách/tác giả, lọc danh mục và khoảng giá, sắp xếp, phân trang, xem chi tiết, thêm/sửa/xóa giỏ, mua ngay, đặt hàng, theo dõi và hủy đơn chờ xử lý, chỉnh hồ sơ, đổi mật khẩu.
- Quản trị: thống kê và doanh thu đơn hoàn thành; CRUD sách, ảnh bìa PNG/JPEG/WebP tối đa 5 MB, danh mục, khách hàng, tài khoản; tìm/lọc/phân trang; chuyển trạng thái đơn theo luồng hợp lệ.
- Giỏ khách lưu trong session; giỏ đã đăng nhập lưu bảng `CartItems`. Đăng nhập hợp nhất sách từ giỏ khách; cùng một sách giữ số lượng lớn hơn của hai giỏ và giới hạn theo tồn kho. Mua ngay không xóa giỏ đang có.
- Đặt hàng lấy lại giá và tồn kho từ database trong transaction. Thiếu hàng sẽ rollback cả đơn. Hủy đơn hoàn kho một lần. Đơn hoàn thành không được hủy.
- Khách chỉ đọc/sửa dữ liệu của mình. Cookie được kiểm tra lại khi tài khoản bị khóa, thay đổi quyền hoặc đổi mật khẩu. Biểu mẫu POST có antiforgery; URL trở về sau đăng nhập được kiểm tra nội bộ.
- Sách đã phát sinh đơn được ngừng bán khi xóa để giữ lịch sử. Không xóa danh mục còn sách hoặc khách hàng đã có đơn; không tự khóa/hạ quyền/xóa tài khoản Admin đang dùng.
- Thanh toán hỗ trợ COD hoặc ghi nhận lựa chọn chuyển khoản; chưa tích hợp cổng thanh toán, tự động xác nhận ngân hàng hay gửi email.

`Program.cs` không gọi `EnsureCreated`, `Migrate` hoặc `SeedData`. `Data/AppDbContext.cs`, các entity và migration được giữ nguyên, không yêu cầu thay đổi cấu trúc BanSachDB cho bản nâng cấp này. Không chạy lại SeedData trên database đang dùng.

## Kiểm thử

```powershell
dotnet test BanSachRazorPages.Tests/BanSachRazorPages.Tests.csproj
```

28 bài kiểm thử gồm giao dịch/tồn kho, hoàn kho, giá từ DB, quyền sở hữu, khóa tài khoản, đổi mật khẩu, CRUD, biểu mẫu Razor, antiforgery và chuyển hướng đăng nhập. Kiểm thử dùng SQLite tạm, không ghi vào `BanSachDB`. Trong đó có hai bài so sánh model với migration/sinh SQL cho MySQL và MariaDB, không kết nối máy chủ.

Để thử thao tác trên dữ liệu mẫu riêng:

```powershell
dotnet run --project BanSachRazorPages.Tests/Preview/Preview.csproj
```

Mở <http://localhost:5091/>. Chỉ trong máy chủ thử này: Admin `admin` / `Admin@123`, khách `user` / `User@123`. SQLite tạm được xóa khi máy chủ thử kết thúc bình thường. Kiểm tra sắp xếp giá trên ứng dụng MariaDB chính; SQLite thử có giới hạn hỗ trợ sắp xếp kiểu decimal.
