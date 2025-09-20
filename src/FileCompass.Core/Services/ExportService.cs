using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FileCompass.Core.Constants;
using FileCompass.Core.Models;

namespace FileCompass.Core.Services;

public class ExportService
{

    public static async Task ExportToCsvAsync(
        IEnumerable<FileEntry> files,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Name,Location,Path,Extension,Size,Size (Bytes),Type,Modified Date,Category");

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = EscapeCsvField(file.Name);
            var location = EscapeCsvField(file.LocationName ?? string.Empty);
            var path = EscapeCsvField(file.RelativePath);
            var extension = EscapeCsvField(file.Extension ?? string.Empty);
            var displaySize = EscapeCsvField(file.DisplaySize);
            var type = file.IsDirectory ? "Folder" : "File";
            var modified = file.ModifiedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
            var category = EscapeCsvField(file.FileCategory);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{name},{location},{path},{extension},{displaySize},{file.Size},{type},{modified},{category}");
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    public static Task ExportToExcelAsync(
        IEnumerable<FileEntry> files,
        string outputPath,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var sheetTitle = title ?? "File Catalog";

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetTitle.Length > 31 ? sheetTitle[..31] : sheetTitle);

        // Header row
        var headers = new[] { "Name", "Location", "Path", "Extension", "Size", "Size (Bytes)", "Type", "Modified", "Category" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        int row = 2;
        foreach (var file in fileList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            worksheet.Cell(row, 1).Value = file.Name;
            worksheet.Cell(row, 2).Value = file.LocationName ?? string.Empty;
            worksheet.Cell(row, 3).Value = file.RelativePath;
            worksheet.Cell(row, 4).Value = file.Extension ?? string.Empty;
            worksheet.Cell(row, 5).Value = file.DisplaySize;
            worksheet.Cell(row, 6).Value = file.Size;
            worksheet.Cell(row, 7).Value = file.IsDirectory ? "Folder" : "File";
            worksheet.Cell(row, 8).Value = file.ModifiedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
            worksheet.Cell(row, 9).Value = file.FileCategory;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Freeze header row
        worksheet.SheetView.FreezeRows(1);

        workbook.SaveAs(outputPath);
        return Task.CompletedTask;
    }

    public static Task ExportToHtmlAsync(
        IEnumerable<FileEntry> files,
        string outputPath,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var reportTitle = title ?? $"{AppConstants.AppName} File Catalog";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <title>{HtmlEncode(reportTitle)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 20px; }");
        sb.AppendLine("    h1 { color: #333; }");
        sb.AppendLine("    .meta { color: #666; margin-bottom: 20px; }");
        sb.AppendLine("    table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("    th { background-color: #4a90d9; color: white; position: sticky; top: 0; }");
        sb.AppendLine("    tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine("    tr:hover { background-color: #f1f1f1; }");
        sb.AppendLine("    .size { text-align: right; }");
        sb.AppendLine("    .folder { color: #c9a227; }");
        sb.AppendLine("    .file { color: #666; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <h1>{HtmlEncode(reportTitle)}</h1>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <p class=\"meta\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Total items: {fileList.Count:N0}</p>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead>");
        sb.AppendLine("      <tr><th>Name</th><th>Location</th><th>Path</th><th>Size</th><th>Type</th><th>Modified</th><th>Category</th></tr>");
        sb.AppendLine("    </thead>");
        sb.AppendLine("    <tbody>");

        foreach (var file in fileList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var typeClass = file.IsDirectory ? "folder" : "file";
            var typeText = file.IsDirectory ? "Folder" : "File";

            sb.AppendLine(CultureInfo.InvariantCulture, $"      <tr>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td class=\"{typeClass}\">{HtmlEncode(file.Name)}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td>{HtmlEncode(file.LocationName ?? string.Empty)}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td>{HtmlEncode(file.RelativePath)}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td class=\"size\">{HtmlEncode(file.DisplaySize)}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td>{typeText}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td>{file.ModifiedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"        <td>{HtmlEncode(file.FileCategory)}</td>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"      </tr>");
        }

        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static string HtmlEncode(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }
}
