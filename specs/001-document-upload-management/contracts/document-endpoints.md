# Contract: Document HTTP Endpoints and UI Routes

All routes require an authenticated cookie session. Authorization is evaluated by
`IDocumentService` using the user id from the `NameIdentifier` claim; unauthorized and missing
documents both return `404 Not Found` so document existence is not revealed.

## HTTP endpoints (minimal API)

### `GET /documents/{id:int}/download`

- **200**: file stream, `Content-Disposition: attachment; filename="<OriginalFileName>"`,
  `Content-Type: <FileType>`, `X-Content-Type-Options: nosniff`. Records a `Download` activity.
- **401/302**: not authenticated → redirect to `/login`.
- **404**: document missing or user not permitted.

### `GET /documents/{id:int}/preview`

- **200**: file stream with `Content-Disposition: inline` for `application/pdf`, `image/jpeg`,
  `image/png` only. Records a `Preview` activity.
- **404**: document missing, user not permitted, or type not previewable.

## Blazor routes (UI contract)

| Route | Page | Access | Purpose |
|-------|------|--------|---------|
| `/documents` | `Pages/Documents.razor` | Any authenticated user | Upload, My Documents (sort/filter), search, Shared with Me |
| `/documents/{id:int}/edit` | `Pages/DocumentEdit.razor` | Uploader | Edit metadata, replace file |
| `/projects/{id:int}` | `Pages/ProjectDetails.razor` (extended) | Project PM/members | Project documents section |
| `/` | `Pages/Index.razor` (extended) | Any authenticated user | Recent Documents widget, document count |
| `/documents/reports` | `Pages/DocumentReports.razor` | `Administrator` policy | Activity reports |

## Service contract — `IDocumentService`

| Method | Purpose |
|--------|---------|
| `UploadDocumentAsync(DocumentUploadRequest, Stream, int userId)` → `DocumentUploadResult` | Validate, scan, store, record, notify |
| `GetUserDocumentsAsync(int userId, DocumentQuery)` → `List<Document>` | My Documents with sort/filter |
| `SearchDocumentsAsync(int userId, string term)` → `List<Document>` | Accessible documents only |
| `GetProjectDocumentsAsync(int projectId, int userId)` → `List<Document>` | Members/PM only |
| `GetSharedWithUserAsync(int userId)` → `List<Document>` | Shared with Me |
| `GetDocumentAsync(int documentId, int userId)` → `Document?` | Null when not permitted |
| `OpenDocumentStreamAsync(int documentId, int userId, string action)` → `(Document, Stream)?` | Download/preview |
| `UpdateMetadataAsync(...)`, `ReplaceFileAsync(...)`, `DeleteDocumentAsync(...)` → `bool` | Owner/PM rules |
| `ShareDocumentAsync(int documentId, int? userId, string? department, int requestingUserId)` → `bool` | Owner only |
| `GetRecentDocumentsAsync(int userId, int count)`, `GetDocumentCountAsync(int userId)` | Dashboard |
| `GetActivityReportAsync(int requestingUserId)` → `DocumentReport?` | Administrators only |
