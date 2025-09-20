using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HDDIndexer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;

namespace HDDIndexer.Services
{
    public class PrintService : IPrintService
    {
        public PrintService()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<bool> PrintCatalogAsync(PrintOptions options)
        {
            // In a real implementation, would send to printer
            // This is a placeholder
            return await Task.FromResult(true);
        }

        public async Task<string> GeneratePdfAsync(IEnumerable<FileEntry> files, PrintOptions options)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"catalog_{Guid.NewGuid()}.pdf");

            await Task.Run(() =>
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var sortedFiles = SortFiles(files, options.SortBy).ToList();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(options.Orientation == PageOrientation.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(options.FontSize));

                        // Header
                        if (options.IncludeHeader)
                        {
                            page.Header().Element(container =>
                            {
                                container.Row(row =>
                                {
                                    row.RelativeItem().Column(column =>
                                    {
                                        if (!string.IsNullOrEmpty(options.Title))
                                        {
                                            column.Item().Text(options.Title).FontSize(16).SemiBold();
                                        }

                                        if (options.IncludeDate)
                                        {
                                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(10);
                                        }
                                    });
                                });
                            });
                        }

                        // Content
                        page.Content().Element(container =>
                        {
                            container.Table(table =>
                            {
                                // Define columns
                                var columns = options.ColumnsToInclude.Any() ? options.ColumnsToInclude : new[] { "Name", "Size", "Type", "Modified" };

                                foreach (var _ in columns)
                                {
                                    table.ColumnsDefinition(def => def.RelativeColumn());
                                }

                                // Add headers
                                table.Header(header =>
                                {
                                    foreach (var column in columns)
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(column).SemiBold();
                                    }
                                });

                                // Add data rows
                                foreach (var file in sortedFiles)
                                {
                                    foreach (var column in columns)
                                    {
                                        var value = GetFilePropertyValue(file, column);
                                        table.Cell().Padding(3).Text(value);
                                    }
                                }
                            });
                        });

                        // Footer
                        if (options.IncludeFooter && options.IncludePageNumbers)
                        {
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        }
                    });
                })
                .GeneratePdf(tempPath);
            });

            return tempPath;
        }

        public async Task<string> GenerateExcelAsync(IEnumerable<FileEntry> files, ExportOptions options)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"catalog_{Guid.NewGuid()}.xlsx");

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("File Catalog");

                // Add headers
                var headers = new[] { "Name", "Path", "Size", "Type", "Modified", "Created" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add data
                var row = 2;
                foreach (var file in files)
                {
                    worksheet.Cells[row, 1].Value = file.FileName;
                    worksheet.Cells[row, 2].Value = file.FilePath;
                    worksheet.Cells[row, 3].Value = file.FileSize ?? 0;
                    worksheet.Cells[row, 4].Value = file.FileType;
                    worksheet.Cells[row, 5].Value = file.DateModified;
                    worksheet.Cells[row, 6].Value = file.DateCreated;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add auto-filter if requested
                if (options.AutoFilter)
                {
                    worksheet.Cells[1, 1, row - 1, headers.Length].AutoFilter = true;
                }

                // Freeze header row if requested
                if (options.FreezeHeader)
                {
                    worksheet.View.FreezePanes(2, 1);
                }

                package.SaveAs(new FileInfo(tempPath));
            });

            return tempPath;
        }

        public async Task<string> GenerateCsvAsync(IEnumerable<FileEntry> files)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"catalog_{Guid.NewGuid()}.csv");

            await Task.Run(() =>
            {
                using var writer = new StreamWriter(tempPath, false, Encoding.UTF8);

                // Write header
                writer.WriteLine("Name,Path,Size,Type,Modified,Created");

                // Write data
                foreach (var file in files)
                {
                    writer.WriteLine($"\"{file.FileName}\",\"{file.FilePath}\",{file.FileSize ?? 0}," +
                                   $"\"{file.FileType}\",\"{file.DateModified:yyyy-MM-dd HH:mm:ss}\"," +
                                   $"\"{file.DateCreated:yyyy-MM-dd HH:mm:ss}\"");
                }
            });

            return tempPath;
        }

        public async Task<string> GenerateHtmlAsync(IEnumerable<FileEntry> files, HtmlOptions options)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"catalog_{Guid.NewGuid()}.html");

            await Task.Run(() =>
            {
                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<title>File Catalog</title>");
                html.AppendLine("<meta charset=\"utf-8\">");

                if (options.Responsive)
                {
                    html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
                }

                // Add CSS
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                html.AppendLine("table { border-collapse: collapse; width: 100%; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                html.AppendLine("th { background-color: #4CAF50; color: white; }");
                html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");

                if (options.IncludeSearch)
                {
                    html.AppendLine("#searchBox { width: 100%; padding: 12px; margin-bottom: 20px; }");
                }

                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                html.AppendLine("<h1>File Catalog</h1>");

                // Add search box if requested
                if (options.IncludeSearch)
                {
                    html.AppendLine("<input type=\"text\" id=\"searchBox\" onkeyup=\"filterTable()\" placeholder=\"Search files...\">");
                }

                // Add table
                html.AppendLine("<table id=\"catalogTable\">");
                html.AppendLine("<thead><tr>");
                html.AppendLine("<th>Name</th><th>Path</th><th>Size</th><th>Type</th><th>Modified</th>");
                html.AppendLine("</tr></thead>");
                html.AppendLine("<tbody>");

                foreach (var file in files)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(file.FileName)}</td>");
                    html.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(file.FilePath)}</td>");
                    html.AppendLine($"<td>{FormatFileSize(file.FileSize ?? 0)}</td>");
                    html.AppendLine($"<td>{file.FileType}</td>");
                    html.AppendLine($"<td>{file.DateModified:yyyy-MM-dd HH:mm:ss}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</tbody>");
                html.AppendLine("</table>");

                // Add JavaScript for search functionality
                if (options.IncludeSearch)
                {
                    html.AppendLine("<script>");
                    html.AppendLine("function filterTable() {");
                    html.AppendLine("  var input = document.getElementById('searchBox');");
                    html.AppendLine("  var filter = input.value.toUpperCase();");
                    html.AppendLine("  var table = document.getElementById('catalogTable');");
                    html.AppendLine("  var tr = table.getElementsByTagName('tr');");
                    html.AppendLine("  for (var i = 1; i < tr.length; i++) {");
                    html.AppendLine("    var td = tr[i].getElementsByTagName('td')[0];");
                    html.AppendLine("    if (td) {");
                    html.AppendLine("      var txtValue = td.textContent || td.innerText;");
                    html.AppendLine("      if (txtValue.toUpperCase().indexOf(filter) > -1) {");
                    html.AppendLine("        tr[i].style.display = '';");
                    html.AppendLine("      } else {");
                    html.AppendLine("        tr[i].style.display = 'none';");
                    html.AppendLine("      }");
                    html.AppendLine("    }");
                    html.AppendLine("  }");
                    html.AppendLine("}");
                    html.AppendLine("</script>");
                }

                html.AppendLine("</body>");
                html.AppendLine("</html>");

                File.WriteAllText(tempPath, html.ToString());
            });

            return tempPath;
        }

        public async Task<PrintPreview> GetPrintPreviewAsync(IEnumerable<FileEntry> files, PrintOptions options)
        {
            var preview = new PrintPreview
            {
                Options = options
            };

            // Generate preview pages (simplified)
            var pageSize = options.Orientation == PageOrientation.Portrait ? 50 : 30;
            var fileList = files.ToList();
            var pageCount = (fileList.Count + pageSize - 1) / pageSize;

            preview.TotalPages = pageCount;

            for (int i = 0; i < pageCount; i++)
            {
                var pageFiles = fileList.Skip(i * pageSize).Take(pageSize);
                var pageContent = GeneratePageContent(pageFiles, options);
                preview.PageContents.Add(pageContent);
            }

            return await Task.FromResult(preview);
        }

        private IEnumerable<FileEntry> SortFiles(IEnumerable<FileEntry> files, SortOrder sortOrder)
        {
            return sortOrder switch
            {
                SortOrder.Name => files.OrderBy(f => f.FileName),
                SortOrder.Size => files.OrderByDescending(f => f.FileSize),
                SortOrder.Date => files.OrderByDescending(f => f.DateModified),
                SortOrder.Type => files.OrderBy(f => f.FileType),
                SortOrder.Path => files.OrderBy(f => f.FilePath),
                _ => files
            };
        }

        private string GetFilePropertyValue(FileEntry file, string propertyName)
        {
            return propertyName.ToLower() switch
            {
                "name" => file.FileName,
                "path" => file.FilePath,
                "size" => FormatFileSize(file.FileSize ?? 0),
                "type" => file.FileType,
                "modified" => file.DateModified.ToString("yyyy-MM-dd HH:mm"),
                "created" => file.DateCreated.ToString("yyyy-MM-dd HH:mm"),
                _ => ""
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GeneratePageContent(IEnumerable<FileEntry> files, PrintOptions options)
        {
            var content = new StringBuilder();

            foreach (var file in files)
            {
                content.AppendLine($"{file.FileName} - {file.FilePath} - {FormatFileSize(file.FileSize ?? 0)}");
            }

            return content.ToString();
        }
    }
}