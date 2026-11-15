
using System;
using System.ComponentModel.DataAnnotations;

namespace BanSachRazorPages.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Một tài khoản có thể liên kết với một khách hàng.
        public Customer? Customer { get; set; }
    }
}
