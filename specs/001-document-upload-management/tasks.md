---

description: "Task list for the document upload and management feature"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/document-endpoints.md,
quickstart.md

**Tests**: No automated tests were requested and the solution has no test project; each story ends
with a manual validation task based on `quickstart.md`.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested
independently. Paths are relative to the repository root; application code lives in
`ContosoDashboard/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story the task belongs to (US1–US7)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuration needed before any feature code.

- [ ] T001 Add `FileStorage:RootPath` (`AppData/uploads`) to `ContosoDashboard/appsettings.json`
- [ ] T002 [P] Ignore `ContosoDashboard/AppData/` in `.gitignore`
- [ ] T003 [P] Verify `ContosoDashboard/ContosoDashboard.csproj` targets `net9.0` and builds with zero errors

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, storage, scanning, authorization helpers, and wiring that every story uses.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Create `Document` entity plus `DocumentCategories` and `DocumentFileTypes` helpers in `ContosoDashboard/Models/Document.cs`
- [ ] T005 [P] Create `DocumentShare` entity in `ContosoDashboard/Models/DocumentShare.cs`
- [ ] T006 [P] Create `DocumentActivity` entity in `ContosoDashboard/Models/DocumentActivity.cs`
- [ ] T007 [P] Append `DocumentShared` and `DocumentAdded` to `NotificationType` in `ContosoDashboard/Models/Notification.cs`
- [ ] T008 Add DbSets, relationships (`Restrict`/`Cascade`), and indexes for the new entities in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [ ] T009 [P] Create `IFileStorageService` and `LocalFileStorageService` (root from config, path traversal guard) in `ContosoDashboard/Services/FileStorageService.cs`
- [ ] T010 [P] Create `IFileScanService` and `BasicFileScanService` (signature checks, executable rejection) in `ContosoDashboard/Services/FileScanService.cs`
- [ ] T011 Create `IDocumentService`/`DocumentService` skeleton with request/result types, access-rule helpers (`CanViewAsync`, `CanEditAsync`, `CanDeleteAsync`), and activity logging helper in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T012 Register `IFileStorageService` (singleton), `IFileScanService` (singleton), and `IDocumentService` (scoped) in `ContosoDashboard/Program.cs`
- [ ] T013 Add development-only guard that recreates the training database when the `Documents` table is missing in `ContosoDashboard/Program.cs`
- [ ] T014 [P] Add `Department` claim at sign-in in `ContosoDashboard/Pages/Login.cshtml.cs`

**Checkpoint**: Application builds and starts; the database contains the new tables.

---

## Phase 3: User Story 1 - Upload and view my documents (Priority: P1) 🎯 MVP

**Goal**: Employees upload up to 10 valid files with metadata and see them in My Documents.

**Independent Test**: Quickstart "MVP validation" steps 1–8.

### Implementation for User Story 1

- [ ] T015 [US1] Implement `UploadDocumentAsync` in `ContosoDashboard/Services/DocumentService.cs`: validate title/category/size/extension, check project membership, scan, generate `{userId}/{projectId|personal}/{guid}{ext}` key, store file, save row, delete stored file if the save fails, log `Upload` activity
- [ ] T016 [US1] Notify other project members (`DocumentAdded`) after a project upload in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T017 [US1] Implement `GetUserDocumentsAsync` (own uploads, newest first, `AsNoTracking`, includes project) and `GetUploadProjectsAsync` (projects the user manages or belongs to) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T018 [US1] Create `ContosoDashboard/Pages/Documents.razor` with `[Authorize]`, upload form (`InputFile multiple` with `@key`, title, category, description, project, tags), client-side required-field validation, max 10 files check
- [ ] T019 [US1] Implement per-file upload in `Documents.razor`: capture metadata before opening the stream, copy to `MemoryStream` with progress updates, clear the file reference, show success/error messages per file
- [ ] T020 [US1] Add the My Documents table (title, category, upload date, size, type, project) to `ContosoDashboard/Pages/Documents.razor`
- [ ] T021 [P] [US1] Add the Documents link to `ContosoDashboard/Shared/NavMenu.razor`
- [ ] T022 [US1] Build and validate User Story 1 using quickstart.md (MVP validation)

**Checkpoint**: MVP complete — upload and My Documents work and are isolated per user.

---

## Phase 4: User Story 2 - Find, download, and preview documents (Priority: P2)

**Goal**: Sort, filter, search, download, and preview accessible documents.

**Independent Test**: Quickstart P2 scenarios.

- [ ] T023 [US2] Add sort (title, date, category, size) and filters (category, project, date range) via `DocumentQuery` to `GetUserDocumentsAsync` in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T024 [US2] Implement `SearchDocumentsAsync` over title, description, tags, uploader, and project restricted by access rules in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T025 [US2] Implement `OpenDocumentStreamAsync` with `Download`/`Preview` activity logging in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T026 [P] [US2] Create download and preview endpoints in `ContosoDashboard/Endpoints/DocumentEndpoints.cs` and map them in `ContosoDashboard/Program.cs`
- [ ] T027 [US2] Add sort/filter controls, search box, and Download/Preview actions to `ContosoDashboard/Pages/Documents.razor`
- [ ] T028 [US2] Validate User Story 2 using quickstart.md

---

## Phase 5: User Story 3 - Project documents for the team (Priority: P3)

**Goal**: Project members see and download project documents; PMs manage them.

**Independent Test**: Quickstart P3 scenarios.

- [ ] T029 [US3] Implement `GetProjectDocumentsAsync` (PM/members only) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T030 [US3] Add a Documents section (list, download, PM delete) to `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T031 [US3] Implement `DeleteDocumentAsync` (uploader or project PM; removes file and row, logs `Delete`) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T032 [US3] Validate User Story 3 using quickstart.md

---

## Phase 6: User Story 4 - Edit, replace, and delete my documents (Priority: P4)

**Goal**: Owners maintain their documents.

**Independent Test**: Quickstart P4 scenarios.

- [ ] T033 [US4] Implement `UpdateMetadataAsync` and `ReplaceFileAsync` (validation + scan, delete old file after new one is stored) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T034 [US4] Create `ContosoDashboard/Pages/DocumentEdit.razor` for metadata edit and file replace
- [ ] T035 [US4] Add Edit and Delete (with confirmation dialog) actions to My Documents in `ContosoDashboard/Pages/Documents.razor`
- [ ] T036 [US4] Validate User Story 4 using quickstart.md

---

## Phase 7: User Story 5 - Share documents (Priority: P5)

**Goal**: Owners share with users or departments; recipients are notified.

**Independent Test**: Quickstart P5 scenarios.

- [ ] T037 [US5] Implement `ShareDocumentAsync` (owner only, user or department, notifications, `Share` activity) and `GetSharedWithUserAsync` in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T038 [US5] Add Share dialog and a Shared with Me tab to `ContosoDashboard/Pages/Documents.razor`
- [ ] T039 [US5] Validate User Story 5 using quickstart.md

---

## Phase 8: User Story 6 - Task and dashboard integration (Priority: P6)

**Goal**: Documents in tasks and on the dashboard.

**Independent Test**: Quickstart P6 scenarios.

- [ ] T040 [US6] Implement `GetTaskDocumentsAsync` and `AttachToTaskAsync` (sets ProjectId from the task) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T041 [US6] Add task documents view, attach, and upload-from-task to `ContosoDashboard/Pages/Tasks.razor`
- [ ] T042 [P] [US6] Implement `GetRecentDocumentsAsync` and `GetDocumentCountAsync` and add the Recent Documents widget and count card to `ContosoDashboard/Pages/Index.razor`
- [ ] T043 [US6] Validate User Story 6 using quickstart.md

---

## Phase 9: User Story 7 - Activity audit and admin reports (Priority: P7)

**Goal**: Administrators see document activity reports.

**Independent Test**: Quickstart P7 scenarios.

- [ ] T044 [US7] Implement `GetActivityReportAsync` (types, top uploaders, activity by day; Administrators only) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T045 [US7] Create `ContosoDashboard/Pages/DocumentReports.razor` with `[Authorize(Policy = "Administrator")]` and add an admin-only NavMenu link
- [ ] T046 [US7] Validate User Story 7 using quickstart.md

---

## Phase 10: Polish & Cross-Cutting Concerns

- [ ] T047 [P] Document the feature (pages, storage location, reset steps) in `README.md`
- [ ] T048 Review build warnings introduced by the feature and run the full quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → user stories (Phases 3–9) → **Polish (Phase 10)**.

### User Story Dependencies

- **US1 (P1)**: depends only on Foundational — MVP.
- **US2 (P2)**: builds on US1 listing; download endpoint reused by US3–US6.
- **US3 (P3)**: needs US2 download endpoint.
- **US4 (P4)**: needs US1; delete logic shared with US3 (T031).
- **US5 (P5)**: needs US1 and US2.
- **US6 (P6)**: needs US1 and US2.
- **US7 (P7)**: needs activity logging from US1/US2.

### Within Each User Story

- Service methods before pages; validation task last.

### Parallel Opportunities

- T002/T003; T004–T007, T009, T010, T014; T021 alongside T018–T020; T026 alongside T023–T025;
  T042 alongside T040–T041.

## Parallel Example: User Story 1

```text
Task: "T021 [P] [US1] Add the Documents link to ContosoDashboard/Shared/NavMenu.razor"
Task: "T018 [US1] Create ContosoDashboard/Pages/Documents.razor upload form"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

**Phases**: Setup → Foundational → US1 only

**Tasks**: T001 - T022 (22 tasks)

**Estimated Time**: 5-7 hours for a developer familiar with ASP.NET Core/Blazor

**Deliverable**: Users can upload documents and view them in My Documents

### Incremental Delivery

1. MVP (T001–T022) → validate → demo.
2. US2 (T023–T028) → search, download, preview.
3. US3–US5 (T029–T039) → collaboration.
4. US6–US7 (T040–T046) → integration and audit.
5. Polish (T047–T048).

## Notes

- Mark each task `[X]` when complete.
- Commit after each phase or logical group.
- Stop at each checkpoint to validate the story independently.
