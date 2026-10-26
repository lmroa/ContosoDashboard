# ContosoDashboard Constitution

## Core Principles

### I. Training-First, Offline-Only Operation (NON-NEGOTIABLE)

ContosoDashboard is a training application and MUST run entirely on a local developer machine
without internet access, cloud subscriptions, or external services.

- Every feature MUST work with only the .NET SDK, SQL Server LocalDB, and the local filesystem.
- No feature MAY introduce a runtime dependency on Azure, third-party APIs, SaaS products, or
  network-hosted services (CDN-hosted UI assets already in use are the only exception).
- Documentation and code comments MUST make clear when a behavior is a training simplification
  and what the production equivalent would be.

Rationale: The application exists to teach Spec-Driven Development; any learner MUST be able to
build and run it offline, at no cost, in a predictable environment.

### II. Infrastructure Abstraction with a Cloud Migration Path

All infrastructure dependencies MUST be accessed through interfaces registered with dependency
injection so that local implementations can be swapped for cloud implementations without
changing business logic.

- File storage MUST go through an `IFileStorageService`-style abstraction; the training
  implementation uses the local filesystem (`System.IO`), the production path is Azure Blob
  Storage.
- Database access MUST go through Entity Framework Core and `ApplicationDbContext`; moving to
  Azure SQL Database MUST require only a connection-string change.
- Authentication MUST stay behind ASP.NET Core authentication middleware so the mock cookie
  provider can be replaced by Microsoft Entra ID without touching pages or services.
- Services and pages MUST NOT reference concrete infrastructure types directly.

Rationale: Learners practice proper abstraction patterns while the app stays offline; production
migration becomes a configuration and implementation swap.

### III. Security by Default and Defense in Depth (NON-NEGOTIABLE)

Every feature MUST enforce security at multiple layers, consistent with the existing codebase.

- Every page that shows or changes user data MUST carry `[Authorize]` (or a role policy:
  `Employee`, `TeamLead`, `ProjectManager`, `Administrator`).
- Every service method that reads or mutates data MUST receive the requesting user's id and
  perform its own authorization check (owner, project manager, project member, or role) before
  returning data. Unauthorized access MUST return `null`, an empty result, or `false`, never
  another user's data (IDOR protection).
- User identity MUST come from authenticated claims, never from query strings, form fields, or
  client-supplied ids.
- All user input (including uploaded file names, sizes, and content types) MUST be validated
  server-side against an allow-list before it is persisted or used in a file path.
- Files on disk MUST be stored under server-generated (GUID-based) paths outside `wwwroot`, and
  MUST be served only through an authorized endpoint.
- Existing security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy) MUST
  be preserved and MUST NOT be weakened by new features.

Rationale: The project teaches secure patterns; a new feature that bypasses them would teach the
wrong lesson and introduce real vulnerabilities.

### IV. Consistent Layered Architecture

New code MUST follow the existing structure and conventions of the solution.

- Layers: `Models/` (EF Core entities), `Data/` (`ApplicationDbContext`, configuration and seed
  data), `Services/` (interface + implementation in the same file, registered as scoped),
  `Pages/` (Blazor Server components and Razor Pages), `Shared/` (layout and navigation).
- Business rules MUST live in services, not in `.razor` pages; pages orchestrate UI only.
- All I/O MUST be asynchronous (`async`/`await`, `...Async` EF Core methods).
- Queries MUST use eager loading (`Include`) or projections to avoid N+1 queries, and frequently
  filtered columns MUST be indexed in `OnModelCreating`.
- The schema is created with `EnsureCreated()` for training; schema changes MUST be reflected in
  the model configuration and seed data so a fresh database is created correctly.
- UI MUST use Bootstrap 5.3 and Bootstrap Icons, matching the look of existing pages, and MUST be
  reachable from `NavMenu` when it is a top-level feature.

Rationale: Consistency keeps the codebase easy to read for learners and lets features compose
with existing dashboard, project, task, and notification capabilities.

### V. Simplicity and Spec-Driven Scope Control

Features MUST be delivered incrementally from an approved specification, plan, and task list.

- Every feature MUST start with `/speckit.specify` and pass `/speckit.plan` and `/speckit.tasks`
  before implementation; implementation MUST trace back to numbered tasks.
- An MVP slice (the highest-priority user story) MUST be independently runnable and testable
  before additional stories are built.
- Out-of-scope items listed in a specification MUST NOT be implemented as part of that feature.
- New dependencies (NuGet packages, JavaScript libraries) MUST be justified in the plan; prefer
  built-in .NET and Blazor capabilities (YAGNI).

Rationale: Small, verified increments are the core lesson of SDD and keep a training codebase
understandable.

## Technology and Platform Constraints

- Runtime: ASP.NET Core with Blazor Server, targeting the .NET SDK installed in the lab environment
  (currently .NET 9; the original codebase targeted .NET 8).
- Data: SQL Server LocalDB via Entity Framework Core (SQLite is an accepted substitute only on ARM64
  machines where LocalDB is unavailable).
- Authentication: Cookie-based mock authentication with claims-based roles; no passwords stored.
- Storage: Local filesystem for binary content, metadata in the database.
- Performance targets for user-facing pages: list and search views SHOULD render within 2 seconds
  for typical training data volumes; long operations (such as uploads) MUST show progress.
- Accessibility: Pages MUST use semantic HTML, labeled form controls, and keyboard-operable
  controls, following the existing Bootstrap components.

## Development Workflow and Quality Gates

- The solution MUST build with zero errors (`dotnet build`) before any commit; new warnings
  SHOULD NOT be introduced.
- Each user story MUST define Given/When/Then acceptance scenarios in `spec.md`, and those
  scenarios MUST be verified manually (or by automated tests when present) before the story is
  considered done.
- Security-relevant behavior (authorization, IDOR protection, input validation) MUST be part of
  every story's acceptance scenarios.
- Work MUST happen on a feature branch created by the Spec Kit workflow; `main` MUST stay runnable.
- Commits SHOULD be small and describe the Spec Kit artifact or task range they deliver.

## Governance

- This constitution supersedes ad-hoc practices for ContosoDashboard. Specifications, plans,
  tasks, and code reviews MUST verify compliance with it; `/speckit.plan` MUST include a
  Constitution Check that passes (or documents a justified exception) before design proceeds.
- Amendments MUST be made by updating this file through `/speckit.constitution`, with a short
  rationale in the commit message and any dependent templates reviewed for consistency.
- Versioning follows semantic versioning: MAJOR for removing or redefining a principle, MINOR for
  adding a principle or materially expanding guidance, PATCH for clarifications and wording.
- Runtime development guidance lives in `README.md`; where it conflicts with this constitution,
  this constitution wins and the README MUST be updated.

**Version**: 1.0.0 | **Ratified**: 2026-10-25 | **Last Amended**: 2026-10-25
