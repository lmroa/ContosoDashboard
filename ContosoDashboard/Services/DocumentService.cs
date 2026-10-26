using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string? Tags { get; set; }
}

public record DocumentUploadResult(bool Success, string Message, Document? Document = null)
{
    public static DocumentUploadResult Failed(string message) => new(false, message);
}

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, Stream content, int requestingUserId);
    Task<List<Document>> GetUserDocumentsAsync(int userId);
    Task<List<Project>> GetUploadProjectsAsync(int userId);
    Task<Document?> GetDocumentAsync(int documentId, int requestingUserId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileScanService _fileScan;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IFileScanService fileScan,
        INotificationService notificationService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _fileScan = fileScan;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, Stream content, int requestingUserId)
    {
        // 1. Validate metadata and file
        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(title))
            return DocumentUploadResult.Failed("A title is required.");
        if (title.Length > 200)
            return DocumentUploadResult.Failed("The title cannot exceed 200 characters.");
        if (!DocumentCategories.IsValid(request.Category))
            return DocumentUploadResult.Failed("Select a valid category.");
        if (request.Description?.Length > 1000)
            return DocumentUploadResult.Failed("The description cannot exceed 1000 characters.");

        if (request.FileSize <= 0 || content.Length == 0)
            return DocumentUploadResult.Failed($"'{request.FileName}' is empty.");
        if (request.FileSize > DocumentFileTypes.MaxFileSize || content.Length > DocumentFileTypes.MaxFileSize)
            return DocumentUploadResult.Failed($"'{request.FileName}' exceeds the 25 MB file size limit.");

        var extension = Path.GetExtension(request.FileName);
        if (!DocumentFileTypes.IsAllowedExtension(extension))
            return DocumentUploadResult.Failed(
                $"'{request.FileName}' is not a supported file type. Supported types: {DocumentFileTypes.SupportedTypesDescription}.");

        var tags = NormalizeTags(request.Tags);
        if (tags?.Length > 500)
            return DocumentUploadResult.Failed("Tags cannot exceed 500 characters.");

        // 2. Authorize: uploading to a project requires being its manager or a member
        if (request.ProjectId.HasValue && !await IsProjectParticipantAsync(request.ProjectId.Value, requestingUserId))
            return DocumentUploadResult.Failed("You can only upload documents to projects you belong to.");

        // 3. Scan for malware before anything is stored
        content.Position = 0;
        var scan = await _fileScan.ScanAsync(content, extension);
        if (!scan.IsClean)
            return DocumentUploadResult.Failed($"'{request.FileName}' was rejected by the security scan: {scan.Reason}");

        // 4. Generate a unique storage key; the user-supplied name is never used in the path
        var folder = request.ProjectId?.ToString() ?? "personal";
        var storageKey = $"{requestingUserId}/{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var mimeType = DocumentFileTypes.AllowedExtensions[extension];

        // 5. Save the file, then the metadata (prevents orphaned database records)
        content.Position = 0;
        await _fileStorage.UploadAsync(content, storageKey, mimeType);

        var document = new Document
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = request.Category,
            Tags = tags,
            OriginalFileName = Path.GetFileName(request.FileName),
            FilePath = storageKey,
            FileType = mimeType,
            FileSize = content.Length,
            UploadedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            UploadedByUserId = requestingUserId,
            ProjectId = request.ProjectId
        };

        try
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Remove the stored file so no orphaned file remains
            _logger.LogError(ex, "Saving document metadata failed; removing stored file {StorageKey}", storageKey);
            _context.ChangeTracker.Clear();
            await _fileStorage.DeleteAsync(storageKey);
            return DocumentUploadResult.Failed($"'{request.FileName}' could not be saved. Please try again.");
        }

        // Record the activity now that the document has its id
        _context.DocumentActivities.Add(CreateActivity(document, requestingUserId, DocumentActions.Upload));
        await _context.SaveChangesAsync();

        // 6. Notify other project members about the new document
        if (document.ProjectId.HasValue)
        {
            await NotifyProjectMembersAsync(document, requestingUserId);
        }

        return new DocumentUploadResult(true, $"'{document.Title}' was uploaded successfully.", document);
    }

    public async Task<List<Document>> GetUserDocumentsAsync(int userId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedDate)
            .Take(500)
            .ToListAsync();
    }

    public async Task<List<Project>> GetUploadProjectsAsync(int userId)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null) return null;

        // Authorization: return nothing when the user may not view the document (IDOR protection)
        return await CanViewAsync(document, requestingUserId) ? document : null;
    }

    // Access rules: uploader, project participant, share recipient, team lead of the uploader's
    // department, or administrator
    private async Task<bool> CanViewAsync(Document document, int userId)
    {
        if (document.UploadedByUserId == userId) return true;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator) return true;

        if (document.ProjectId.HasValue && await IsProjectParticipantAsync(document.ProjectId.Value, userId))
            return true;

        var sharedWithUser = await _context.DocumentShares.AnyAsync(s =>
            s.DocumentId == document.DocumentId &&
            (s.SharedWithUserId == userId ||
             (user.Department != null && s.SharedWithDepartment == user.Department)));
        if (sharedWithUser) return true;

        if (user.Role == UserRole.TeamLead && user.Department != null)
        {
            return await _context.Users.AnyAsync(u =>
                u.UserId == document.UploadedByUserId && u.Department == user.Department);
        }

        return false;
    }

    private async Task<bool> IsProjectParticipantAsync(int projectId, int userId)
    {
        return await _context.Projects.AnyAsync(p =>
            p.ProjectId == projectId &&
            (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)));
    }

    private async Task NotifyProjectMembersAsync(Document document, int uploaderId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == document.ProjectId);
        if (project == null) return;

        var recipients = project.ProjectMembers.Select(pm => pm.UserId)
            .Append(project.ProjectManagerId)
            .Where(id => id != uploaderId)
            .Distinct();

        foreach (var recipientId in recipients)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = recipientId,
                Title = "New project document",
                Message = $"'{document.Title}' was added to project {project.Name}.",
                Type = NotificationType.DocumentAdded,
                Priority = NotificationPriority.Informational
            });
        }
    }

    private static DocumentActivity CreateActivity(Document document, int userId, string action) => new()
    {
        DocumentId = document.DocumentId,
        DocumentTitle = document.Title,
        FileType = document.FileType,
        UserId = userId,
        Action = action,
        Timestamp = DateTime.UtcNow
    };

    private static string? NormalizeTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return null;

        var normalized = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct();

        var result = string.Join(", ", normalized);
        return result.Length == 0 ? null : result;
    }
}
