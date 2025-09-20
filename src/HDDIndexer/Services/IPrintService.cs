using System.Collections.Generic;
using System.Threading.Tasks;
using HDDIndexer.Models;

namespace HDDIndexer.Services
{
    public interface IPrintService
    {
        Task<bool> PrintCatalogAsync(PrintOptions options);
        Task<string> GeneratePdfAsync(IEnumerable<FileEntry> files, PrintOptions options);
        Task<string> GenerateExcelAsync(IEnumerable<FileEntry> files, ExportOptions options);
        Task<string> GenerateCsvAsync(IEnumerable<FileEntry> files);
        Task<string> GenerateHtmlAsync(IEnumerable<FileEntry> files, HtmlOptions options);
        Task<PrintPreview> GetPrintPreviewAsync(IEnumerable<FileEntry> files, PrintOptions options);
    }

    public class PrintOptions
    {
        public PrintFormat Format { get; set; }
        public PrintLayout Layout { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IncludeHeader { get; set; } = true;
        public bool IncludeFooter { get; set; } = true;
        public bool IncludePageNumbers { get; set; } = true;
        public bool IncludeDate { get; set; } = true;
        public SortOrder SortBy { get; set; }
        public List<string> ColumnsToInclude { get; set; } = new();
        public int FontSize { get; set; } = 10;
        public PageOrientation Orientation { get; set; }
    }

    public class ExportOptions
    {
        public bool IncludeCharts { get; set; }
        public bool IncludeSummary { get; set; }
        public bool AutoFilter { get; set; } = true;
        public bool FreezeHeader { get; set; } = true;
    }

    public class HtmlOptions
    {
        public bool IncludeNavigation { get; set; } = true;
        public bool IncludeSearch { get; set; } = true;
        public string CssTheme { get; set; } = "default";
        public bool Responsive { get; set; } = true;
    }

    public class PrintPreview
    {
        public int TotalPages { get; set; }
        public List<string> PageContents { get; set; } = new();
        public PrintOptions Options { get; set; } = new();
    }

    public enum PrintFormat
    {
        Detailed,
        Compact,
        Summary,
        Custom
    }

    public enum PrintLayout
    {
        List,
        Tree,
        Grid
    }

    public enum SortOrder
    {
        Name,
        Size,
        Date,
        Type,
        Path
    }

    public enum PageOrientation
    {
        Portrait,
        Landscape
    }
}