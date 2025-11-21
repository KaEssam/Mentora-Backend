namespace Mentora.Core.Data;

public class File
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FilePath { get; set; }
    public string? PublicUrl { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public string? UploadedById { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties will be configured in Infrastructure layer
    // to avoid circular references with Core layer
    // TODO: INTEGRATION - Navigation Properties - Consider using specification pattern or queries to avoid navigation properties in Core entities
}
