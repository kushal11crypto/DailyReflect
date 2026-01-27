using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Models
{
    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Date of the journal entry (Date only, no time component)
        /// Only one entry allowed per day
        /// </summary>
        [Required]
        public DateTime EntryDate { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if content is in Markdown format
        /// </summary>
        public bool IsMarkdown { get; set; }

        /// <summary>
        /// System-generated timestamp when entry was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// System-generated timestamp when entry was last updated
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Mood tracking - Primary mood is required
        [Required]
        public int PrimaryMoodId { get; set; }

        [ForeignKey("PrimaryMoodId")]
        public virtual Mood PrimaryMood { get; set; } = null!;

        // Secondary moods are optional (up to 2)
        public int? SecondaryMood1Id { get; set; }

        [ForeignKey("SecondaryMood1Id")]
        public virtual Mood? SecondaryMood1 { get; set; }

        public int? SecondaryMood2Id { get; set; }

        [ForeignKey("SecondaryMood2Id")]
        public virtual Mood? SecondaryMood2 { get; set; }

        // Category (optional)
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Tags (Many-to-Many relationship)
        public virtual ICollection<JournalEntryTag> JournalEntryTags { get; set; } = new List<JournalEntryTag>();

        /// <summary>
        /// Calculated word count for analytics
        /// </summary>
        [NotMapped]
        public int WordCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return 0;

                return Content.Split(new[] { ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }
    }
}
