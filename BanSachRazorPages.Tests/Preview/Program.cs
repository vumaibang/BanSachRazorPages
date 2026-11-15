using BanSachRazorPages.Data;
using BanSachRazorPages.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Chỉ dùng để kiểm tra giao diện trên SQLite tạm, không chạm vào BanSachDB.
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var database = Path.Combine(Path.GetTempPath(), $"BanSachRazor-preview-{Guid.NewGuid():N}.db");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
    Args = args, ApplicationName = typeof(RazorHosting).Assembly.GetName().Name,
    ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot"), EnvironmentName = "Development"
});
builder.Services.AddBookStore(options => options.UseSqlite($"Data Source={database}"));
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
var app = builder.Build();
app.UseBookStore();
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync()) {
    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(db);
}
try { await app.RunAsync("http://localhost:5091"); }
finally {
    await app.DisposeAsync();
    SqliteConnection.ClearAllPools();
    foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(database + suffix)) File.Delete(database + suffix);
}
