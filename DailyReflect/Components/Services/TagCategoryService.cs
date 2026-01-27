using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DailyReflect.Components.Models;
using DailyReflect.Components.Data;

namespace DailyReflect.Components.Services
{
    public interface ITagCategoryService
    {
        // Tag operations
        Task<List<Tag>> GetAllTagsAsync();
        Task<List<Tag>> GetPreBuiltTagsAsync();
        Task<List<Tag>> GetCustomTagsAsync();
        Task<Tag?> GetTagByIdAsync(int id);
        Task<Tag> CreateTagAsync(string tagName, bool isCustom = true);
        Task<bool> DeleteTagAsync(int id);

        // Category operations
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(string name, string description = "");
        Task<bool> DeleteCategoryAsync(int id);

        // Mood operations
        Task<List<Mood>> GetAllMoodsAsync();
        Task<List<Mood>> GetMoodsByCategoryAsync(MoodCategory category);
        Task<Mood?> GetMoodByIdAsync(int id);
    }

    /// <summary>
    /// Service for managing tags, categories, and moods
    /// </summary>
    public class TagCategoryService : ITagCategoryService
    {
        private readonly ApplicationDbContext _context;

        public TagCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Tag Operations

        /// <summary>
        /// Gets all tags (both pre-built and custom)
        /// </summary>
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            try
            {
                return await _context.Tags
                    .OrderBy(t => !t.IsCustom)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all tags: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets only pre-built tags
        /// </summary>
        public async Task<List<Tag>> GetPreBuiltTagsAsync()
        {
            try
            {
                return await _context.Tags
                    .Where(t => !t.IsCustom)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving pre-built tags: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets only custom tags created by user
        /// </summary>
        public async Task<List<Tag>> GetCustomTagsAsync()
        {
            try
            {
                return await _context.Tags
                    .Where(t => t.IsCustom)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving custom tags: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets tag by ID
        /// </summary>
        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            try
            {
                return await _context.Tags.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving tag with ID {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a new tag (custom or pre-built)
        /// </summary>
        public async Task<Tag> CreateTagAsync(string tagName, bool isCustom = true)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(tagName))
                    throw new ArgumentException("Tag name cannot be empty");

                tagName = tagName.Trim();

                // Check if tag already exists
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());

                if (existingTag != null)
                    return existingTag;

                // Create new tag
                var newTag = new Tag
                {
                    Name = tagName,
                    IsCustom = isCustom,
                    CreatedAt = DateTime.Now
                };

                _context.Tags.Add(newTag);
                await _context.SaveChangesAsync();

                return newTag;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Database error while creating tag: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating tag: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a tag (only custom tags can be deleted)
        /// </summary>
        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                    return false;

                // Prevent deletion of pre-built tags
                if (!tag.IsCustom)
                    throw new InvalidOperationException("Cannot delete pre-built tags");

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting tag with ID {id}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Category Operations

        /// <summary>
        /// Gets all categories
        /// </summary>
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            try
            {
                return await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all categories: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets category by ID
        /// </summary>
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _context.Categories.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving category with ID {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a new category
        /// </summary>
        public async Task<Category> CreateCategoryAsync(string name, string description = "")
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Category name cannot be empty");

                name = name.Trim();

                // Check if category already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

                if (existingCategory != null)
                    return existingCategory;

                // Create new category
                var newCategory = new Category
                {
                    Name = name,
                    Description = description?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                _context.Categories.Add(newCategory);
                await _context.SaveChangesAsync();

                return newCategory;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Database error while creating category: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating category: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a category
        /// </summary>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return false;

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting category with ID {id}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Mood Operations

        /// <summary>
        /// Gets all moods
        /// </summary>
        public async Task<List<Mood>> GetAllMoodsAsync()
        {
            try
            {
                return await _context.Moods
                    .OrderBy(m => m.Category)
                    .ThenBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all moods: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets moods filtered by category (Positive, Neutral, Negative)
        /// </summary>
        public async Task<List<Mood>> GetMoodsByCategoryAsync(MoodCategory category)
        {
            try
            {
                return await _context.Moods
                    .Where(m => m.Category == category)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving moods for category {category}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets mood by ID
        /// </summary>
        public async Task<Mood?> GetMoodByIdAsync(int id)
        {
            try
            {
                return await _context.Moods.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving mood with ID {id}: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
