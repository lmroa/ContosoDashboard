# Research: Document Upload and Management

**Feature**: `001-document-upload-management` | **Date**: 2026-10-25

All Technical Context unknowns are resolved below. Each entry records the decision, the rationale,
and the alternatives considered.

## R1. File storage location and layout

- **Decision**: Store files on the local filesystem under a configurable root
  (`FileStorage:RootPath`, default `AppData/uploads` relative to the content root, outside
  `wwwroot`) using the relative key `{userId}/{projectId|personal}/{guid}{ext}`. Only the relative
  key is stored in the database.
- **Rationale**: Satisfies offline operation (Principle I) and keeps files unreachable by static
  file middleware (Principle III). The same key works as an Azure Blob name later (Principle II).
- **Alternatives considered**: Storing bytes in SQL Server (`varbinary(max)`) — rejected: bloats the
  training database and does not teach the storage abstraction. Storing under `wwwroot` — rejected:
  bypasses authorization.

## R2. Storage abstraction

- **Decision**: `IFileStorageService` with `UploadAsync(Stream, string key, string contentType)`,
  `DownloadAsync(string key)`, `DeleteAsync(string key)`, `GetUrlAsync(string key, TimeSpan)`;
  training implementation `LocalFileStorageService` (System.IO), registered as a singleton.
  The caller generates the key before calling `UploadAsync` (stakeholder requirement: generate unique
  path → save file → save metadata).
- **Rationale**: Matches the stakeholder interface and README guidance; DI swap to
  `AzureBlobStorageService` needs no business-logic change. Path traversal is prevented by rejecting
  keys that resolve outside the root.
- **Alternatives considered**: Letting the storage service generate names — rejected because the
  service would then dictate database keys and the ordering rule would be harder to verify.

## R3. Malware scanning offline

- **Decision**: `IFileScanService` with a training implementation `BasicFileScanService` that
  (1) checks the file signature ("magic bytes") matches the extension (PDF `%PDF`, PNG, JPEG,
  ZIP-based Office `PK`, legacy Office OLE `D0 CF 11 E0`, text = no NUL bytes) and (2) rejects
  executable signatures (`MZ`, ELF, shebang scripts). Any exception means "rejected".
- **Rationale**: Implements the clarified requirement (FR-008) without external services; a
  production `IFileScanService` (for example, Microsoft Defender API) can be swapped in.
- **Alternatives considered**: Windows Defender command-line (`MpCmdRun.exe`) — rejected: not
  available on every lab machine and non-portable. No scanning — rejected: violates FR-008.

## R4. Serving downloads and previews from Blazor Server

- **Decision**: Minimal API endpoints `GET /documents/{id}/download` and `GET /documents/{id}/preview`
  mapped in `Program.cs` via `Endpoints/DocumentEndpoints.cs`, protected with
  `RequireAuthorization()`, resolving the user id from the cookie claims and calling
  `IDocumentService` for the authorization check. Preview returns `inline` disposition only for PDF
  and images; download returns `attachment` with the original file name.
- **Rationale**: Blazor Server components cannot return file responses; HTTP endpoints reuse the same
  cookie authentication and keep authorization in the service layer (Principle III).
- **Alternatives considered**: JS interop streaming (`DotNetStreamReference`) — works for download but
  not for in-browser preview via URL; a Razor Page per action — more boilerplate for the same result.

## R5. Blazor file upload handling

- **Decision**: `InputFile` with `multiple`, a `@key` that changes after each upload, metadata
  captured into locals before opening the stream, `OpenReadStream(25 MB)` copied to a `MemoryStream`,
  and the `IBrowserFile` reference cleared afterwards. Progress is reported per file (bytes copied /
  total) during the copy.
- **Rationale**: Follows the stakeholder Blazor-specific requirements and avoids disposed-stream
  errors in Blazor Server.
- **Alternatives considered**: Posting to an HTTP upload endpoint with JavaScript `fetch` —
  rejected for the MVP; more moving parts and duplicated validation.

## R6. Schema creation with `EnsureCreated()`

- **Decision**: Keep `EnsureCreated()`. Add a development-only guard in `Program.cs`: if the database
  exists but the `Documents` table is missing (database created before this feature), delete and
  recreate the training database so new tables and seed data are applied.
- **Rationale**: `EnsureCreated()` does nothing when the database already exists, which would break
  the feature for every learner who ran the app before. The data is seed-only training data.
- **Alternatives considered**: Introducing EF Core migrations — rejected as a larger architectural
  change for a training app (Principle V); manual `sqllocaldb` reset — kept only as a documented
  fallback in quickstart.md.

## R7. Department claim for team-based permissions

- **Decision**: Add a `Department` claim at login (when the user has a department) and resolve
  department from the database in the service layer when needed.
- **Rationale**: Stakeholder notes state the Department claim is required for team sharing and Team
  Lead visibility; services still verify against the database so stale claims cannot widen access.
- **Alternatives considered**: Claims only — rejected (claims could be stale); database only — works,
  but the claim is required by the stakeholder document.

## R8. Search and listing performance

- **Decision**: Server-side filtering with EF Core `IQueryable` (`Contains` on title, description,
  tags, uploader name, project name), `AsNoTracking`, indexes on `UploadedByUserId`, `ProjectId`,
  `Category`, `UploadedDate`; results capped at 500 rows.
- **Rationale**: Meets the 2-second targets for ≤500 documents on LocalDB without a search engine.
- **Alternatives considered**: Full-text search — rejected: requires extra LocalDB configuration;
  in-memory filtering — rejected: does not scale and loads unauthorized rows.

## R9. Testing approach

- **Decision**: Manual acceptance testing following `quickstart.md` scenarios (the solution has no
  test project). The build must stay at zero errors.
- **Rationale**: Matches the existing project and the constitution quality gates; adding a test
  project is outside this feature's scope.
- **Alternatives considered**: Adding xUnit + bUnit tests — deferred to a future feature.
