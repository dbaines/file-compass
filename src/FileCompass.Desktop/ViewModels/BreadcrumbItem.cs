namespace FileCompass.Desktop.ViewModels;

/// <summary>
/// Represents an item in the breadcrumb navigation trail.
/// </summary>
/// <param name="FileId">The file ID of the folder, or null for the root.</param>
/// <param name="Name">The display name of the folder.</param>
public record BreadcrumbItem(long? FileId, string Name);
