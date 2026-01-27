using DailyReflect.Components.Data;
using DailyReflect.Components.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DailyReflect.Components.Services
{
    /// <summary>
    /// Interface for journal service operations
    /// </summary>
    public interface IJournalService
    {
        Task<JournalEntry?> GetEntryByDateAsync(DateTime date);
        Task<JournalEntry?> GetEntryByIdAsync(int id);
        Task<List<JournalEntry>> GetAllEntriesAsync();
        Task<List<JournalEntry>> GetEntriesPaginatedAsync(int page, int pageSize);
        Task<int> GetTotalEntriesCountAsync();
        Task<JournalEntry> CreateOrUpdateEntryAsync(JournalEntry entry);
        Task<bool> DeleteEntryAsync(int id);
        Task<bool> DeleteEntryByDateAsync(DateTime date);
        Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm);
        Task<List<JournalEntry>> FilterEntriesAsync(DateTime? startDate, DateTime? endDate,
            List<int>? moodIds, List<int>? tagIds);
        Task UpdateStreakDataAsync(DateTime entryDate);
        Task<StreakData> GetStreakDataAsync();
    }

    /// <summary>
    /// Service for managing journal entries and related operations
    /// </summary>
    public class JournalService : IJournalService
    {
        private readonly ApplicationDbContext _context;

        public JournalService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets journal entry for a specific date
        /// </summary>
        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            try
            {
                // Normalize date to remove time component
                var normalizedDate = date.Date;

                return await _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .FirstOrDefaultAsync(e => e.EntryDate.Date == normalizedDate);
            }
            catch (Exception ex)
            {
                // Log exception (implement logging in production)
                throw new Exception($"Error retrieving entry for date {date}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets journal entry by ID
        /// </summary>
        public async Task<JournalEntry?> GetEntryByIdAsync(int id)
        {
            try
            {
                return await _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving entry with ID {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all journal entries ordered by date descending
        /// </summary>
        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            try
            {
                return await _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .OrderByDescending(e => e.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets paginated journal entries
        /// </summary>
        public async Task<List<JournalEntry>> GetEntriesPaginatedAsync(int page, int pageSize)
        {
            try
            {
                return await _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .OrderByDescending(e => e.EntryDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving paginated entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets total count of journal entries
        /// </summary>
        public async Task<int> GetTotalEntriesCountAsync()
        {
            return await _context.JournalEntries.CountAsync();
        }

        /// <summary>
        /// Creates new entry or updates existing entry for the day
        /// </summary>
        public async Task<JournalEntry> CreateOrUpdateEntryAsync(JournalEntry entry)
        {
            try
            {
                // Normalize date
                entry.EntryDate = entry.EntryDate.Date;

                // Check if entry exists for this date
                var existingEntry = await _context.JournalEntries
                    .Include(e => e.JournalEntryTags)
                    .FirstOrDefaultAsync(e => e.EntryDate.Date == entry.EntryDate);

                if (existingEntry != null)
                {
                    // Update existing entry
                    existingEntry.Title = entry.Title;
                    existingEntry.Content = entry.Content;
                    existingEntry.IsMarkdown = entry.IsMarkdown;
                    existingEntry.PrimaryMoodId = entry.PrimaryMoodId;
                    existingEntry.SecondaryMood1Id = entry.SecondaryMood1Id;
                    existingEntry.SecondaryMood2Id = entry.SecondaryMood2Id;
                    existingEntry.CategoryId = entry.CategoryId;
                    existingEntry.UpdatedAt = DateTime.Now;

                    // Update tags
                    existingEntry.JournalEntryTags.Clear();
                    foreach (var tag in entry.JournalEntryTags)
                    {
                        existingEntry.JournalEntryTags.Add(new JournalEntryTag
                        {
                            JournalEntryId = existingEntry.Id,
                            TagId = tag.TagId
                        });
                    }

                    _context.JournalEntries.Update(existingEntry);
                    await _context.SaveChangesAsync();

                    // Update streak
                    await UpdateStreakDataAsync(existingEntry.EntryDate);

                    return existingEntry;
                }
                else
                {
                    // Create new entry
                    entry.CreatedAt = DateTime.Now;
                    entry.UpdatedAt = DateTime.Now;

                    _context.JournalEntries.Add(entry);
                    await _context.SaveChangesAsync();

                    // Update streak
                    await UpdateStreakDataAsync(entry.EntryDate);

                    return entry;
                }
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Database error while saving entry: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating/updating entry: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes journal entry by ID
        /// </summary>
        public async Task<bool> DeleteEntryAsync(int id)
        {
            try
            {
                var entry = await _context.JournalEntries.FindAsync(id);
                if (entry == null)
                    return false;

                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();

                // Recalculate streak after deletion
                await RecalculateStreaksAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting entry with ID {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes journal entry by date
        /// </summary>
        public async Task<bool> DeleteEntryByDateAsync(DateTime date)
        {
            try
            {
                var entry = await GetEntryByDateAsync(date);
                if (entry == null)
                    return false;

                return await DeleteEntryAsync(entry.Id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting entry for date {date}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Searches entries by title or content
        /// </summary>
        public async Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllEntriesAsync();

                var lowerSearchTerm = searchTerm.ToLower();

                return await _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .Where(e => e.Title.ToLower().Contains(lowerSearchTerm) ||
                                e.Content.ToLower().Contains(lowerSearchTerm))
                    .OrderByDescending(e => e.EntryDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Filters entries by date range, moods, and tags
        /// </summary>
        public async Task<List<JournalEntry>> FilterEntriesAsync(
            DateTime? startDate,
            DateTime? endDate,
            List<int>? moodIds,
            List<int>? tagIds)
        {
            try
            {
                var query = _context.JournalEntries
                    .Include(e => e.PrimaryMood)
                    .Include(e => e.SecondaryMood1)
                    .Include(e => e.SecondaryMood2)
                    .Include(e => e.Category)
                    .Include(e => e.JournalEntryTags)
                        .ThenInclude(jet => jet.Tag)
                    .AsQueryable();

                // Filter by date range
                if (startDate.HasValue)
                    query = query.Where(e => e.EntryDate >= startDate.Value.Date);

                if (endDate.HasValue)
                    query = query.Where(e => e.EntryDate <= endDate.Value.Date);

                // Filter by moods
                if (moodIds != null && moodIds.Any())
                {
                    query = query.Where(e =>
                        moodIds.Contains(e.PrimaryMoodId) ||
                        (e.SecondaryMood1Id.HasValue && moodIds.Contains(e.SecondaryMood1Id.Value)) ||
                        (e.SecondaryMood2Id.HasValue && moodIds.Contains(e.SecondaryMood2Id.Value)));
                }

                // Filter by tags
                if (tagIds != null && tagIds.Any())
                {
                    query = query.Where(e => e.JournalEntryTags.Any(jet => tagIds.Contains(jet.TagId)));
                }

                return await query.OrderByDescending(e => e.EntryDate).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error filtering entries: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates streak data when a new entry is created
        /// </summary>
        public async Task UpdateStreakDataAsync(DateTime entryDate)
        {
            try
            {
                var streakData = await _context.StreakData.FirstOrDefaultAsync();
                if (streakData == null)
                {
                    streakData = new StreakData { Id = 1 };
                    _context.StreakData.Add(streakData);
                }

                var normalizedDate = entryDate.Date;

                if (streakData.LastEntryDate == null)
                {
                    // First entry ever
                    streakData.CurrentStreak = 1;
                    streakData.LongestStreak = 1;
                    streakData.LastEntryDate = normalizedDate;
                    streakData.LongestStreakStartDate = normalizedDate;
                    streakData.LongestStreakEndDate = normalizedDate;
                }
                else
                {
                    var daysDiff = (normalizedDate - streakData.LastEntryDate.Value.Date).Days;

                    if (daysDiff == 1)
                    {
                        // Consecutive day
                        streakData.CurrentStreak++;
                        streakData.LastEntryDate = normalizedDate;

                        if (streakData.CurrentStreak > streakData.LongestStreak)
                        {
                            streakData.LongestStreak = streakData.CurrentStreak;
                            streakData.LongestStreakEndDate = normalizedDate;
                        }
                    }
                    else if (daysDiff == 0)
                    {
                        // Same day update - no change to streak
                        streakData.LastEntryDate = normalizedDate;
                    }
                    else
                    {
                        // Streak broken
                        streakData.CurrentStreak = 1;
                        streakData.LastEntryDate = normalizedDate;
                    }
                }

                streakData.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating streak data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recalculates all streak data from scratch
        /// </summary>
        private async Task RecalculateStreaksAsync()
        {
            try
            {
                var allEntries = await _context.JournalEntries
                    .OrderBy(e => e.EntryDate)
                    .Select(e => e.EntryDate.Date)
                    .ToListAsync();

                var streakData = await _context.StreakData.FirstOrDefaultAsync();
                if (streakData == null)
                {
                    streakData = new StreakData { Id = 1 };
                    _context.StreakData.Add(streakData);
                }

                if (!allEntries.Any())
                {
                    streakData.CurrentStreak = 0;
                    streakData.LongestStreak = 0;
                    streakData.LastEntryDate = null;
                    streakData.LongestStreakStartDate = null;
                    streakData.LongestStreakEndDate = null;
                }
                else
                {
                    int currentStreak = 1;
                    int longestStreak = 1;
                    DateTime? longestStart = allEntries[0];
                    DateTime? longestEnd = allEntries[0];

                    for (int i = 1; i < allEntries.Count; i++)
                    {
                        if ((allEntries[i] - allEntries[i - 1]).Days == 1)
                        {
                            currentStreak++;
                        }
                        else
                        {
                            if (currentStreak > longestStreak)
                            {
                                longestStreak = currentStreak;
                                longestStart = allEntries[i - currentStreak];
                                longestEnd = allEntries[i - 1];
                            }
                            currentStreak = 1;
                        }
                    }

                    // Check final streak
                    if (currentStreak > longestStreak)
                    {
                        longestStreak = currentStreak;
                        longestStart = allEntries[allEntries.Count - currentStreak];
                        longestEnd = allEntries[allEntries.Count - 1];
                    }

                    // Check if current streak is still active
                    var today = DateTime.Now.Date;
                    var lastEntry = allEntries[allEntries.Count - 1];
                    var daysSinceLastEntry = (today - lastEntry).Days;

                    streakData.CurrentStreak = daysSinceLastEntry <= 1 ? currentStreak : 0;
                    streakData.LongestStreak = longestStreak;
                    streakData.LastEntryDate = lastEntry;
                    streakData.LongestStreakStartDate = longestStart;
                    streakData.LongestStreakEndDate = longestEnd;
                }

                streakData.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error recalculating streaks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets current streak data
        /// </summary>
        public async Task<StreakData> GetStreakDataAsync()
        {
            try
            {
                var streakData = await _context.StreakData.FirstOrDefaultAsync();
                if (streakData == null)
                {
                    streakData = new StreakData { Id = 1 };
                    _context.StreakData.Add(streakData);
                    await _context.SaveChangesAsync();
                }
                return streakData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving streak data: {ex.Message}", ex);
            }
        }
    }
}