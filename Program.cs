using BanSachRazorPages.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");
var version = Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "10.4.32");
ServerVersion serverVersion = builder.Configuration["Database:ServerType"] == "MySql"
    ? new MySqlServerVersion(version) : new MariaDbServerVersion(version);
builder.Services.AddBookStore(options => options.UseMySql(connection, serverVersion));
var app = builder.Build();
app.UseBookStore();
app.Run();

public partial class Program { }
