using BanSachRazorPages.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BanSachRazorPages.Tests;

public class MigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreservedMySqlMigrationsMatchCurrentModels(bool mariaDb)
    {
        // Sinh SQL và so sánh snapshot mà không cần kết nối MySQL.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=MigrationCheck;User=test;", mariaDb
                ? new MariaDbServerVersion(new Version(10, 4, 32))
                : new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;
        using var db = new AppDbContext(options);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE `CartItems`", script);
        Assert.Contains("CREATE TABLE `Orders`", script);
        Assert.Equal(4, db.Database.GetMigrations().Count());
    }
}
