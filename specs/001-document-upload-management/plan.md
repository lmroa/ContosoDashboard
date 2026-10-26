# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-10-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

Add a document library to ContosoDashboard: employees upload up to 10 files at a time (PDF, Office,
text, JPEG/PNG, ≤25 MB each) with title, category, optional description/project/tags; files are
validated, scanned, and stored on the local filesystem through `IFileStorageService`, with metadata
in SQL Server LocalDB. Users browse, sort, filter, search, download, and preview documents they are
authorized to see; project members see project documents; owners edit, replace, delete, and share;
the dashboard, tasks, and notifications integrate with documents; administrators get activity
reports. The MVP is User Story 1 (upload + My Documents).

## Technical Context

**Language/Version**: C# 13 / .NET 9 (`net9.0`; original code targeted .NET 8)

**Primary Dependencies**: ASP.NET Core Blazor Server, Razor Pages (login), Entity Framework Core 9
SQL Server provider, Bootstrap 5.3 + Bootstrap Icons (CDN, already in use). No new NuGet packages.

**Storage**: SQL Server LocalDB (metadata) via EF Core `EnsureCreated()`; local filesystem
`AppData/uploads` (file content) behind `IFileStorageService`.

**Testing**: Manual acceptance scenarios from `quickstart.md`; `dotnet build` with zero errors
(no test project exists in the solution).

**Target Platform**: Windows/macOS/Linux developer machine, browser on `http://localhost:5000`.

**Project Type**: Server-rendered web application (single ASP.NET Core project).

**Performance Goals**: 25 MB upload ≤ 30 s locally; lists and search ≤ 2 s for 500 documents;
preview ≤ 3 s.

**Constraints**: Offline-only; files outside `wwwroot`; GUID-based storage names; service-level
authorization on every operation; max 25 MB per file, 10 files per upload.

**Scale/Scope**: Training data (4 seeded users, 1 project); designed for hundreds of documents per
user. 3 new entities, 3 new services, 3 new pages, 2 HTTP endpoints, 4 existing pages extended.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. Training-first, offline | No cloud SDKs or external services; local storage and local scan only | ✅ Pass |
| II. Infrastructure abstraction | `IFileStorageService` and `IFileScanService` via DI; relative storage keys | ✅ Pass |
| III. Security by default | `[Authorize]` on pages, `RequireAuthorization` on endpoints, service-level checks with requesting user id, allow-listed types, GUID paths outside `wwwroot`, 404 for unauthorized | ✅ Pass |
| IV. Layered architecture | Models / Data / Services / Pages; async EF Core with `Include`/indexes; Bootstrap UI; NavMenu entry | ✅ Pass |
| V. Simplicity & scope | MVP = US1; no new packages; out-of-scope items excluded | ✅ Pass |
| Quality gates | Build with zero errors; Given/When/Then scenarios include access denial | ✅ Pass |

**Post-design re-check (after Phase 1)**: ✅ Pass. Two design points were checked explicitly:
the minimal API endpoints for file delivery (needed because Blazor components cannot return file
responses; they still go through `IDocumentService` for authorization), and the development-only
database recreate guard for `EnsureCreated()` (affects training data only). Neither violates the
constitution, so Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entities and access rules
├── quickstart.md        # Phase 1 validation guide
├── contracts/
│   └── document-endpoints.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Created by /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Models/
│   ├── Document.cs              # NEW: Document entity + DocumentCategories + DocumentFileTypes
│   ├── DocumentShare.cs         # NEW
│   ├── DocumentActivity.cs      # NEW
│   └── Notification.cs          # MODIFIED: DocumentShared, DocumentAdded types
├── Data/
│   └── ApplicationDbContext.cs  # MODIFIED: DbSets, relationships, indexes
├── Services/
│   ├── FileStorageService.cs    # NEW: IFileStorageService + LocalFileStorageService
│   ├── FileScanService.cs       # NEW: IFileScanService + BasicFileScanService
│   └── DocumentService.cs       # NEW: IDocumentService + DocumentService (+ request/result types)
├── Endpoints/
│   └── DocumentEndpoints.cs     # NEW: /documents/{id}/download and /preview
├── Pages/
│   ├── Documents.razor          # NEW: upload, My Documents, search, Shared with Me
│   ├── DocumentEdit.razor       # NEW (US4)
│   ├── DocumentReports.razor    # NEW (US7)
│   ├── ProjectDetails.razor     # MODIFIED (US3)
│   ├── Tasks.razor              # MODIFIED (US6)
│   ├── Index.razor              # MODIFIED (US6)
│   └── Login.cshtml.cs          # MODIFIED: Department claim
├── Shared/
│   └── NavMenu.razor            # MODIFIED: Documents link
├── Program.cs                   # MODIFIED: DI, endpoints, DB recreate guard
└── appsettings.json             # MODIFIED: FileStorage:RootPath
.gitignore                       # MODIFIED: ignore AppData/uploads
```

**Structure Decision**: Keep the existing single ASP.NET Core project and its layer folders; add one
`Endpoints/` folder for the file-delivery HTTP endpoints. No new projects.

## Implementation Approach

1. **Foundation**: entities, DbContext configuration, storage and scan services, document service
   skeleton with authorization helpers, DI registration, database guard, Department claim.
2. **US1 (MVP)**: upload workflow in `DocumentService` (validate count/size/type → scan → generate
   key → store file → save row → activity → project notifications; delete stored file if the database
   save fails), `Documents.razor` upload form with progress and My Documents table, NavMenu link.
3. **US2**: sort/filter/search queries, download and preview endpoints.
4. **US3**: project documents section and PM delete; new-document notifications.
5. **US4**: edit metadata, replace file, delete with confirmation.
6. **US5**: sharing (user or department), Shared with Me, notifications.
7. **US6**: task attachment/upload, dashboard widget and count.
8. **US7**: activity reports page for administrators.
9. **Polish**: README update, quickstart validation, build warnings review.

## Complexity Tracking

No constitution violations to justify.
