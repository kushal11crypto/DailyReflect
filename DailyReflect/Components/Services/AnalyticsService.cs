using DailyReflect.Components.Data;
using DailyReflect.Components.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflect.Components.Services
{
    public class MoodDistribution
    {
        public MoodCategory Category { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MoodFrequency
    {
        public string MoodName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Emoji { get; set; } = string.Empty;
    }

    public class TagUsage
    {
        public string TagName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class WordCountTrend
    {
        public DateTime Date { get; set; }
        public int WordCount { get; set; }
    }

    public class AnalyticsSummary
    {
        public List<MoodDistribution> MoodDistribution { get; set; } = new();
        public MoodFrequency? MostFrequentMood { get; set; }
        public List<TagUsage> MostUsedTags { get; set; } = new();
        public List<TagUsage> TagBreakdown { get; set; } = new();
        public List<WordCountTrend> WordCountTrends { get; set; } = new();
        public int TotalEntries { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int MissedDays { get; set; }
        public double AverageWordCount { get; set; }
    }

    /// <summary>
    /// Interface for analytics operations
    /// </summary>
    public interface IAnalyticsService
    {
        Task<AnalyticsSummary> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<MoodDistribution>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<MoodFrequency?> GetMostFrequentMoodAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<TagUsage>> GetMostUsedTagsAsync(int topN = 10, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<WordCountTrend>> GetWordCountTrendsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetMissedDaysAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    /// <summary>
    /// Service for generating analytics and insights
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJournalService _journalService;

        public AnalyticsService(ApplicationDbContext context, IJournalService journalService)
        {
            _context = context;
            _journalService = journalService;
        }

        /// <summary>
        /// Gets complete analytics summary
        /// </summary>
        public async Task<AnalyticsSummary> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var summary = new AnalyticsSummary
                {
                    MoodDistribution = await GetMoodDistributionAsync(startDate, endDate),
                    MostFrequentMood = await GetMostFrequentMoodAsync(startDate, endDate),
                    MostUsedTags = await GetMostUsedTagsAsync(10, startDate, endDate),
                    TagBreakdown = await GetTagBreakdownAsync(startDate, endDate),
                    WordCountTrends = await GetWordCountTrendsAsync(startDate, endDate),
                    MissedDays = await GetMissedDaysAsync(startDate, endDate)
                };

                // Get streak data
                var streakData = await _journalService.GetStreakDataAsync();
                summary.CurrentStreak = streakData.CurrentStreak;
                summary.LongestStreak = streakData.LongestStreak;

                // Calculate total entries and average word count
                var entries = await GetFilteredEntriesAsync(startDate, endDate);
                summary.TotalEntries = entries.Count;
                summary.AverageWordCount = entries.Any() ? entries.Average(e => e.WordCount) : 0;

                return summary;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating analytics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets mood distribution (Positive, Neutral, Negative percentages)
        /// </summary>
        public async Task<List<MoodDistribution>> GetMoodDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = await GetFilteredEntriesAsync(startDate, endDate);

                if (!entries.Any())
                    return new List<MoodDistribution>();

                var totalEntries = entries.Count;
                var moodCounts = new Dictionary<MoodCategory, int>
                {
                    { MoodCategory.Positive, 0 },
                    { MoodCategory.Neutral, 0 },
                    { MoodCategory.Negative, 0 }
                };

                foreach (var entry in entries)
                {
                    if (entry.PrimaryMood != null)
                        moodCounts[entry.PrimaryMood.Category]++;
                }

                return moodCounts.Select(mc => new MoodDistribution
                {
                    Category = mc.Key,
                    Count = mc.Value,
                    Percentage = Math.Round((double)mc.Value / totalEntries * 100, 2)
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating mood distribution: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the most frequently recorded mood
        /// </summary>
        public async Task<MoodFrequency?> GetMostFrequentMoodAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = await GetFilteredEntriesAsync(startDate, endDate);

                if (!entries.Any())
                    return null;

                var moodCounts = new Dictionary<int, int>();

                foreach (var entry in entries)
                {
                    // Count primary mood
                    if (!moodCounts.ContainsKey(entry.PrimaryMoodId))
                        moodCounts[entry.PrimaryMoodId] = 0;
                    moodCounts[entry.PrimaryMoodId]++;

                    // Count secondary moods
                    if (entry.SecondaryMood1Id.HasValue)
                    {
                        if (!moodCounts.ContainsKey(entry.SecondaryMood1Id.Value))
                            moodCounts[entry.SecondaryMood1Id.Value] = 0;
                        moodCounts[entry.SecondaryMood1Id.Value]++;
                    }

                    if (entry.SecondaryMood2Id.HasValue)
                    {
                        if (!moodCounts.ContainsKey(entry.SecondaryMood2Id.Value))
                            moodCounts[entry.SecondaryMood2Id.Value] = 0;
                        moodCounts[entry.SecondaryMood2Id.Value]++;
                    }
                }

                var mostFrequent = moodCounts.OrderByDescending(mc => mc.Value).FirstOrDefault();
                var mood = await _context.Moods.FindAsync(mostFrequent.Key);

                if (mood == null)
                    return null;

                return new MoodFrequency
                {
                    MoodName = mood.Name,
                    Count = mostFrequent.Value,
                    Emoji = mood.Emoji
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating most frequent mood: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets most used tags
        /// </summary>
        public async Task<List<TagUsage>> GetMostUsedTagsAsync(int topN = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = await GetFilteredEntriesAsync(startDate, endDate);

                if (!entries.Any())
                    return new List<TagUsage>();

                var totalEntries = entries.Count;
                var tagCounts = new Dictionary<string, int>();

                foreach (var entry in entries)
                {
                    foreach (var jet in entry.JournalEntryTags)
                    {
                        var tagName = jet.Tag.Name;
                        if (!tagCounts.ContainsKey(tagName))
                            tagCounts[tagName] = 0;
                        tagCounts[tagName]++;
                    }
                }

                return tagCounts
                    .OrderByDescending(tc => tc.Value)
                    .Take(topN)
                    .Select(tc => new TagUsage
                    {
                        TagName = tc.Key,
                        Count = tc.Value,
                        Percentage = Math.Round((double)tc.Value / totalEntries * 100, 2)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating most used tags: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets tag breakdown (percentage of entries per tag)
        /// </summary>
        private async Task<List<TagUsage>> GetTagBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await GetMostUsedTagsAsync(int.MaxValue, startDate, endDate);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating tag breakdown: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets word count trends over time
        /// </summary>
        public async Task<List<WordCountTrend>> GetWordCountTrendsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var entries = await GetFilteredEntriesAsync(startDate, endDate);

                return entries
                    .OrderBy(e => e.EntryDate)
                    .Select(e => new WordCountTrend
                    {
                        Date = e.EntryDate,
                        WordCount = e.WordCount
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating word count trends: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates missed days (days without entries in a date range)
        /// </summary>
        public async Task<int> GetMissedDaysAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? await GetFirstEntryDateAsync() ?? DateTime.Now;
                var end = endDate ?? DateTime.Now;

                var totalDays = (end.Date - start.Date).Days + 1;
                var entries = await GetFilteredEntriesAsync(startDate, endDate);
                var entryDays = entries.Count;

                return Math.Max(0, totalDays - entryDays);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating missed days: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to get filtered entries
        /// </summary>
        private async Task<List<JournalEntry>> GetFilteredEntriesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.JournalEntries
                .Include(e => e.PrimaryMood)
                .Include(e => e.SecondaryMood1)
                .Include(e => e.SecondaryMood2)
                .Include(e => e.JournalEntryTags)
                    .ThenInclude(jet => jet.Tag)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.EntryDate >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.EntryDate <= endDate.Value.Date);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Gets the date of the first journal entry
        /// </summary>
        private async Task<DateTime?> GetFirstEntryDateAsync()
        {
            var firstEntry = await _context.JournalEntries
                .OrderBy(e => e.EntryDate)
                .FirstOrDefaultAsync();

            return firstEntry?.EntryDate;
        }
    }
}
