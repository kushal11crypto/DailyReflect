using DailyReflect.Components.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;

namespace DailyReflect.Components.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use a simple local string for migration generation 
            // (This doesn't affect your mobile app's actual database)
            optionsBuilder.UseSqlite("Data Source=migration_temp.db");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
    public class ApplicationDbContext : DbContext
    {
        public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
        public DbSet<Mood> Moods { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<JournalEntryTag> JournalEntryTags { get; set; } = null!;
        public DbSet<UserSettings> UserSettings { get; set; } = null!;
        public DbSet<StreakData> StreakData { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Creates a new instance of ApplicationDbContext with the database path set to DailyReflect.db
        /// in the same directory structure as the original code.
        /// </summary>
        public static ApplicationDbContext CreateInstance()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DailyReflect",
                "DailyReflect.db");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            var context = new ApplicationDbContext(optionsBuilder.Options);
            // Optionally, ensure database is created and migrated
            context.Database.Migrate(); // or context.Database.EnsureCreated();

            return context;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and seed data as before...
            // (Include your existing OnModelCreating code here)

            // Example: Keep your existing configuration
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasIndex(e => e.EntryDate).IsUnique();

                entity.HasOne(e => e.PrimaryMood)
                    .WithMany(m => m.PrimaryMoodEntries)
                    .HasForeignKey(e => e.PrimaryMoodId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SecondaryMood1)
                    .WithMany(m => m.SecondaryMood1Entries)
                    .HasForeignKey(e => e.SecondaryMood1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SecondaryMood2)
                    .WithMany(m => m.SecondaryMood2Entries)
                    .HasForeignKey(e => e.SecondaryMood2Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.JournalEntries)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<JournalEntryTag>(entity =>
            {
                entity.HasKey(jet => new { jet.JournalEntryId, jet.TagId });

                entity.HasOne(jet => jet.JournalEntry)
                    .WithMany(je => je.JournalEntryTags)
                    .HasForeignKey(jet => jet.JournalEntryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(jet => jet.Tag)
                    .WithMany(t => t.JournalEntryTags)
                    .HasForeignKey(jet => jet.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // Seed initial data if needed
            SeedData(modelBuilder);
        }


/// <summary>
/// Seeds initial data for moods and pre-built tags
/// </summary>
private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Moods
            var moods = new List<Mood>
            {
                // Positive Moods
                new Mood { Id = 1, Name = "Happy", Category = MoodCategory.Positive, Emoji = "😊" },
                new Mood { Id = 2, Name = "Excited", Category = MoodCategory.Positive, Emoji = "🤩" },
                new Mood { Id = 3, Name = "Relaxed", Category = MoodCategory.Positive, Emoji = "😌" },
                new Mood { Id = 4, Name = "Grateful", Category = MoodCategory.Positive, Emoji = "🙏" },
                new Mood { Id = 5, Name = "Confident", Category = MoodCategory.Positive, Emoji = "😎" },

                // Neutral Moods
                new Mood { Id = 6, Name = "Calm", Category = MoodCategory.Neutral, Emoji = "😐" },
                new Mood { Id = 7, Name = "Thoughtful", Category = MoodCategory.Neutral, Emoji = "🤔" },
                new Mood { Id = 8, Name = "Curious", Category = MoodCategory.Neutral, Emoji = "🧐" },
                new Mood { Id = 9, Name = "Nostalgic", Category = MoodCategory.Neutral, Emoji = "😌" },
                new Mood { Id = 10, Name = "Bored", Category = MoodCategory.Neutral, Emoji = "😑" },

                // Negative Moods
                new Mood { Id = 11, Name = "Sad", Category = MoodCategory.Negative, Emoji = "😔" },
                new Mood { Id = 12, Name = "Angry", Category = MoodCategory.Negative, Emoji = "😠" },
                new Mood { Id = 13, Name = "Stressed", Category = MoodCategory.Negative, Emoji = "😰" },
                new Mood { Id = 14, Name = "Lonely", Category = MoodCategory.Negative, Emoji = "😞" },
                new Mood { Id = 15, Name = "Anxious", Category = MoodCategory.Negative, Emoji = "😟" }
            };

            modelBuilder.Entity<Mood>().HasData(moods);

            // Seed Pre-built Tags
            var preBuildTags = new[]
            {
                "Work", "Career", "Studies", "Family", "Friends", "Relationships",
                "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies",
                "Travel", "Nature", "Finance", "Spirituality", "Birthday",
                "Holiday", "Vacation", "Celebration", "Exercise", "Reading",
                "Writing", "Cooking", "Meditation", "Yoga", "Music", "Shopping",
                "Parenting", "Projects", "Planning", "Reflection"
            };

            var tags = new List<Tag>();
            for (int i = 0; i < preBuildTags.Length; i++)
            {
                tags.Add(new Tag
                {
                    Id = i + 1,
                    Name = preBuildTags[i],
                    IsCustom = false,
                    CreatedAt = DateTime.Now
                });
            }

            modelBuilder.Entity<Tag>().HasData(tags);

            // Seed default UserSettings
            modelBuilder.Entity<UserSettings>().HasData(new UserSettings
            {
                Id = 1,
                RequirePassword = false,
                Theme = "Light",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            // Seed default StreakData
            modelBuilder.Entity<StreakData>().HasData(new StreakData
            {
                Id = 1,
                CurrentStreak = 0,
                LongestStreak = 0,
                UpdatedAt = DateTime.Now
            });
        }
    }
}
 