namespace FileCompass.Core.Models;

public class Tag
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string Colour { get; set; } = "#808080";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
