using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Stored as text for simplicity (see DocumentCategories)
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = DocumentCategories.Other;

    // Comma-separated, lower-cased tags
    [MaxLength(500)]
    public string? Tags { get; set; }

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    // Relative storage key: {userId}/{projectId|personal}/{guid}{ext}
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    // MIME type (Office types can be long)
    [Required]
    [MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    // Navigation properties
    [ForeignKey("UploadedByUserId")]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("TaskId")]
    public virtual TaskItem? RelatedTask { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
}

public static class DocumentCategories
{
    public const string ProjectDocuments = "Project Documents";
    public const string TeamResources = "Team Resources";
    public const string PersonalFiles = "Personal Files";
    public const string Reports = "Reports";
    public const string Presentations = "Presentations";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ProjectDocuments, TeamResources, PersonalFiles, Reports, Presentations, Other
    };

    public static bool IsValid(string? category) => category != null && All.Contains(category);
}

public static class DocumentFileTypes
{
    public const long MaxFileSize = 25L * 1024 * 1024; // 25 MB
    public const int MaxFilesPerUpload = 10;

    // Allow-list of extensions and the MIME type stored for each
    public static readonly IReadOnlyDictionary<string, string> AllowedExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    public static string AcceptAttribute => string.Join(",", AllowedExtensions.Keys);

    public static string SupportedTypesDescription => "PDF, Word, Excel, PowerPoint, text, JPEG and PNG";

    public static bool IsAllowedExtension(string? extension) =>
        !string.IsNullOrEmpty(extension) && AllowedExtensions.ContainsKey(extension);

    public static bool IsPreviewable(string mimeType) =>
        mimeType is "application/pdf" or "image/jpeg" or "image/png";

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / 1024d / 1024d:0.0} MB";
        if (bytes >= 1024) return $"{bytes / 1024d:0.0} KB";
        return $"{bytes} B";
    }
}
