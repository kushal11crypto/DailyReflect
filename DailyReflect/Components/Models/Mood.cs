using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Models
{
    public enum MoodCategory
    {
        Positive,
        Neutral,
        Negative
    }

    /// <summary>
    /// Mood entity for tracking emotional states
    /// </summary>
    public class Mood
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public MoodCategory Category { get; set; }

        [MaxLength(10)]
        public string Emoji { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<JournalEntry> PrimaryMoodEntries { get; set; } = new List<JournalEntry>();
        public virtual ICollection<JournalEntry> SecondaryMood1Entries { get; set; } = new List<JournalEntry>();
        public virtual ICollection<JournalEntry> SecondaryMood2Entries { get; set; } = new List<JournalEntry>();
    }

}
