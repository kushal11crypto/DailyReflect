using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Models
{
    public class StreakData
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Current consecutive days of journaling
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Maximum streak ever achieved
        /// </summary>
        public int LongestStreak { get; set; }

        /// <summary>
        /// Date of the last journal entry
        /// </summary>
        public DateTime? LastEntryDate { get; set; }

        /// <summary>
        /// Start date of the longest streak
        /// </summary>
        public DateTime? LongestStreakStartDate { get; set; }

        /// <summary>
        /// End date of the longest streak
        /// </summary>
        public DateTime? LongestStreakEndDate { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
