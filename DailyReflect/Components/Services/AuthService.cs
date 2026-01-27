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
    /// </summary>
    public interface IAuthService
    {
        Task<bool> IsPasswordSetAsync();
        Task<bool> ValidatePasswordAsync(string password);
        Task<bool> SetPasswordAsync(string password);
        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
        Task<bool> RemovePasswordAsync(string password);
        Task<UserSettings> GetSettingsAsync();
        Task<bool> UpdateThemeAsync(string theme);
    }

    /// <summary>
    /// Service for authentication and user settings
    /// Uses BCrypt for password hashing
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if password protection is enabled
        /// </summary>
        public async Task<bool> IsPasswordSetAsync()
        {
            try
            {
                var settings = await GetSettingsAsync();
                return settings.RequirePassword && !string.IsNullOrEmpty(settings.PasswordHash);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking password status: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates entered password against stored hash
        /// </summary>
        public async Task<bool> ValidatePasswordAsync(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                    return false;

                var settings = await GetSettingsAsync();

                if (string.IsNullOrEmpty(settings.PasswordHash))
                    return false;

                // Verify password using BCrypt
                return BCrypt.Net.BCrypt.Verify(password, settings.PasswordHash);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error validating password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets initial password for the application
        /// </summary>
        public async Task<bool> SetPasswordAsync(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password cannot be empty");

                if (password.Length < 4)
                    throw new ArgumentException("Password must be at least 4 characters long");

                var settings = await GetSettingsAsync();

                // Hash password using BCrypt
                settings.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                settings.RequirePassword = true;
                settings.UpdatedAt = DateTime.Now;

                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error setting password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Changes existing password
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                // Validate old password
                if (!await ValidatePasswordAsync(oldPassword))
                    return false;

                // Validate new password
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password cannot be empty");

                if (newPassword.Length < 4)
                    throw new ArgumentException("New password must be at least 4 characters long");

                var settings = await GetSettingsAsync();

                // Hash new password
                settings.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                settings.UpdatedAt = DateTime.Now;

                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error changing password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Removes password protection
        /// </summary>
        public async Task<bool> RemovePasswordAsync(string password)
        {
            try
            {
                // Validate current password
                if (!await ValidatePasswordAsync(password))
                    return false;

                var settings = await GetSettingsAsync();

                settings.PasswordHash = null;
                settings.RequirePassword = false;
                settings.UpdatedAt = DateTime.Now;

                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets user settings
        /// </summary>
        public async Task<UserSettings> GetSettingsAsync()
        {
            try
            {
                var settings = await _context.UserSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create default settings if none exist
                    settings = new UserSettings
                    {
                        Id = 1,
                        RequirePassword = false,
                        Theme = "Light",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.UserSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return settings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates theme preference (Light/Dark)
        /// </summary>
        public async Task<bool> UpdateThemeAsync(string theme)
        {
            try
            {
                if (theme != "Light" && theme != "Dark")
                    throw new ArgumentException("Theme must be 'Light' or 'Dark'");

                var settings = await GetSettingsAsync();
                settings.Theme = theme;
                settings.UpdatedAt = DateTime.Now;

                _context.UserSettings.Update(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating theme: {ex.Message}", ex);
            }
        }
    }
}
