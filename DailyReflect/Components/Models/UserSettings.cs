using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Models
{
    public class UserSettings
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Hashed password using BCrypt
        /// </summary>
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Indicates if password protection is enabled
        /// </summary>
        public bool RequirePassword { get; set; }

        /// <summary>
        /// Theme preference: "Light" or "Dark"
        /// </summary>
        [MaxLength(50)]
        public string Theme { get; set; } = "Light";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
