using DailyReflect.Components.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties; // Added for TextAlignment
using iText.Layout.Borders; // For borders
using Markdig;

namespace DailyReflect.Components.Services
{
    public interface IPdfExportService
    {
        Task<byte[]> ExportEntriesAsync(List<JournalEntry> entries, string title = "Journal Export");
        Task<byte[]> ExportSingleEntryAsync(JournalEntry entry);
    }

    /// <summary>
    /// Service for exporting journal entries to PDF
    /// Uses iText7 library
    /// </summary>
    public class PdfExportService : IPdfExportService
    {
        public async Task<byte[]> ExportEntriesAsync(List<JournalEntry> entries, string title = "Journal Export")
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    using var writer = new PdfWriter(memoryStream);
                    using var pdfDoc = new PdfDocument(writer);
                    using var document = new Document(pdfDoc);

                    // Set up fonts
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // Add title
                    var titleParagraph = new Paragraph(title)
                        .SetFont(boldFont)
                        .SetFontSize(24)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetMarginBottom(20);
                    document.Add(titleParagraph);

                    // Add export date
                    var exportDate = new Paragraph($"Exported on: {DateTime.Now:MMMM dd, yyyy}")
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetMarginBottom(30);
                    document.Add(exportDate);

                    // Add horizontal line
                    var line = new Paragraph()
                        .SetBorder(new SolidBorder(ColorConstants.GRAY, 1))
                        .SetMarginBottom(20);
                    document.Add(line);

                    // Add each entry
                    foreach (var entry in entries.OrderBy(e => e.EntryDate))
                    {
                        AddEntryToDocument(document, entry, boldFont, regularFont);

                        // Add separator between entries
                        var separator = new Paragraph()
                            .SetBorder(new DashedBorder(ColorConstants.LIGHT_GRAY, 1))
                            .SetMarginTop(15)
                            .SetMarginBottom(15);
                        document.Add(separator);
                    }

                    document.Close();
                    return memoryStream.ToArray();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error generating PDF: {ex.Message}", ex);
                }
            });
        }

        public async Task<byte[]> ExportSingleEntryAsync(JournalEntry entry)
        {
            return await ExportEntriesAsync(new List<JournalEntry> { entry }, $"Journal Entry - {entry.EntryDate:MMMM dd, yyyy}");
        }

        private void AddEntryToDocument(Document document, JournalEntry entry, PdfFont boldFont, PdfFont regularFont)
        {
            // Entry date
            var dateParagraph = new Paragraph(entry.EntryDate.ToString("dddd, MMMM dd, yyyy"))
                .SetFont(boldFont)
                .SetFontSize(16)
                .SetFontColor(new DeviceRgb(51, 51, 51));
            document.Add(dateParagraph);

            // Title
            if (!string.IsNullOrWhiteSpace(entry.Title))
            {
                var titleParagraph = new Paragraph(entry.Title)
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginTop(5)
                    .SetFontColor(new DeviceRgb(85, 85, 85));
                document.Add(titleParagraph);
            }

            // Moods
            var moodText = $"Mood: {entry.PrimaryMood?.Emoji} {entry.PrimaryMood?.Name}";
            if (entry.SecondaryMood1 != null)
                moodText += $", {entry.SecondaryMood1.Emoji} {entry.SecondaryMood1.Name}";
            if (entry.SecondaryMood2 != null)
                moodText += $", {entry.SecondaryMood2.Emoji} {entry.SecondaryMood2.Name}";

            var moodParagraph = new Paragraph(moodText)
                .SetFont(regularFont)
                .SetFontSize(10)
                .SetMarginTop(5)
                .SetFontColor(new DeviceRgb(102, 102, 102));
            document.Add(moodParagraph);

            // Category
            if (entry.Category != null)
            {
                var categoryParagraph = new Paragraph($"Category: {entry.Category.Name}")
                    .SetFont(regularFont)
                    .SetFontSize(10)
                    .SetFontColor(new DeviceRgb(102, 102, 102));
                document.Add(categoryParagraph);
            }

            // Tags
            if (entry.JournalEntryTags.Any())
            {
                var tags = string.Join(", ", entry.JournalEntryTags.Select(jet => jet.Tag.Name));
                var tagsParagraph = new Paragraph($"Tags: {tags}")
                    .SetFont(regularFont)
                    .SetFontSize(10)
                    .SetMarginBottom(10)
                    .SetFontColor(new DeviceRgb(102, 102, 102));
                document.Add(tagsParagraph);
            }

            // Content
            var content = entry.Content;

            // Convert Markdown to HTML then strip HTML tags for plain text
            if (entry.IsMarkdown)
            {
                var pipeline = new MarkdownPipelineBuilder().Build();
                var htmlContent = Markdown.ToHtml(content, pipeline);
                content = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", string.Empty);
            }

            // Add content paragraph with Justify alignment
            var contentParagraph = new Paragraph(content)
                .SetFont(regularFont)
                .SetFontSize(11)
                .SetMarginTop(10)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.JUSTIFIED_ALL);
            document.Add(contentParagraph);

            // Metadata
            var metadataParagraph = new Paragraph(
                $"Created: {entry.CreatedAt:MMM dd, yyyy h:mm tt} | " +
                $"Updated: {entry.UpdatedAt:MMM dd, yyyy h:mm tt} | " +
                $"Words: {entry.WordCount}")
                .SetFont(regularFont)
                .SetFontSize(8)
                .SetMarginTop(10)
                .SetFontColor(new DeviceRgb(153, 153, 153))
                .SetItalic();
            document.Add(metadataParagraph);
        }
    }
}