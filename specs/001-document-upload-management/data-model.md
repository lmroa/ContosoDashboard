# Data Model: Document Upload and Management

**Feature**: `001-document-upload-management` | **Date**: 2026-10-25

Conventions follow the existing models: integer keys, data annotations for lengths, navigation
properties declared `virtual`, UTC timestamps.

## Document (new) — `Models/Document.cs`

| Field | Type | Rules |
|-------|------|-------|
| DocumentId | int (PK, identity) | Integer for consistency with User/Project keys |
| Title | string | Required, max 200 |
| Description | string? | Max 1000 |
| Category | string | Required, max 50, one of `DocumentCategories.All` (stored as text) |
| Tags | string? | Max 500, comma-separated, trimmed, lower-cased for search |
| OriginalFileName | string | Required, max 255, display/download name only |
| FilePath | string | Required, max 500, unique; relative storage key `{userId}/{projectId|personal}/{guid}{ext}` |
| FileType | string | Required, max 255 (MIME type) |
| FileSize | long | Required, 1 … 26,214,400 bytes |
| UploadedDate | DateTime | Set by service (UTC) |
| UpdatedDate | DateTime | Set by service (UTC) |
| UploadedByUserId | int (FK → User) | From authenticated claims, `Restrict` on delete |
| ProjectId | int? (FK → Project) | Uploader must be PM or member; `Restrict` on delete |
| TaskId | int? (FK → TaskItem) | When set, ProjectId = task's ProjectId; `Restrict` on delete |

Indexes: `UploadedByUserId`, `ProjectId`, `Category`, `UploadedDate`, unique `FilePath`.

Static helper `DocumentCategories`: `Project Documents`, `Team Resources`, `Personal Files`,
`Reports`, `Presentations`, `Other`.

Static helper `DocumentFileTypes`: allowed extensions → MIME type map (`.pdf`, `.doc`, `.docx`,
`.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.jpg`, `.jpeg`, `.png`), `MaxFileSize = 25 MB`,
`MaxFilesPerUpload = 10`, `IsPreviewable(mime)` for PDF and images.

## DocumentShare (new) — `Models/DocumentShare.cs`

| Field | Type | Rules |
|-------|------|-------|
| DocumentShareId | int (PK) | |
| DocumentId | int (FK → Document) | `Cascade` on delete (share disappears with the document) |
| SharedWithUserId | int? (FK → User) | Exactly one of SharedWithUserId / SharedWithDepartment is set |
| SharedWithDepartment | string? | Max 100, matches `User.Department` |
| SharedByUserId | int (FK → User) | Must be the document owner; `Restrict` on delete |
| SharedDate | DateTime | UTC |

Indexes: `(DocumentId)`, `(SharedWithUserId)`, `(SharedWithDepartment)`.

## DocumentActivity (new) — `Models/DocumentActivity.cs`

| Field | Type | Rules |
|-------|------|-------|
| DocumentActivityId | int (PK) | |
| DocumentId | int | Plain column (no FK) so records survive document deletion |
| DocumentTitle | string | Max 200, snapshot at time of action |
| FileType | string | Max 255, snapshot for reports |
| UserId | int (FK → User) | Actor, `Restrict` on delete |
| Action | string | Max 20: `Upload`, `Download`, `Preview`, `Delete`, `Share`, `Update`, `Replace` |
| Timestamp | DateTime | UTC |

Indexes: `(UserId)`, `(Timestamp)`, `(Action)`.

## Changes to existing models

- `NotificationType`: append `DocumentShared` and `DocumentAdded` at the end of the enum (existing
  numeric values unchanged).
- `ApplicationDbContext`: add `DbSet<Document> Documents`, `DbSet<DocumentShare> DocumentShares`,
  `DbSet<DocumentActivity> DocumentActivities`, relationships with `DeleteBehavior.Restrict` toward
  User/Project/TaskItem, and the indexes above.

## Access rules (enforced in `DocumentService`)

A user can **view/download** a document when any of the following is true:

1. They uploaded it.
2. It belongs to a project they manage or are a member of.
3. It is shared with them directly or with their department.
4. They are a Team Lead and the uploader is in their department.
5. They are an Administrator.

A user can **edit/replace** a document only if they uploaded it. A user can **delete** a document if
they uploaded it, or they are the Project Manager of the document's project.

## Lifecycle

`Selected → Validated (type, size, count) → Scanned → Stored (file) → Recorded (database row +
activity) → [Updated | Replaced | Shared]* → Deleted (file + row removed, activity kept)`.
Failure after Stored and before Recorded deletes the stored file.
