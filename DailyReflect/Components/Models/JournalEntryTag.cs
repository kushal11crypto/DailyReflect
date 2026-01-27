using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Models
{
    public class JournalEntryTag
    {
        public int JournalEntryId { get; set; }
        public virtual JournalEntry JournalEntry { get; set; } = null!;

        public int TagId { get; set; }
        public virtual Tag Tag { get; set; } = null!;
    }
}
