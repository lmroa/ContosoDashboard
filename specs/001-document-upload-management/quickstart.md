# Quickstart: Document Upload and Management

Validation guide for the feature. See [data-model.md](data-model.md) and
[contracts/document-endpoints.md](contracts/document-endpoints.md) for details.

## Prerequisites

- .NET SDK 9 (project targets `net9.0`), SQL Server LocalDB.
- Sample files: a PDF under 25 MB, a PNG, a `.txt`, a file over 25 MB, and a renamed `.exe`
  (for example `fake.pdf`).

## Run

```powershell
cd ContosoDashboard
dotnet build
dotnet run --launch-profile http
```

Open `http://localhost:5000`. On first run after this feature, the development guard recreates the
training database if the `Documents` table is missing. Fallback if the database is in a bad state:

```powershell
sqllocaldb stop mssqllocaldb
sqllocaldb delete mssqllocaldb
```

Uploaded files are stored under `ContosoDashboard/AppData/uploads/` (ignored by Git).

## MVP validation (User Story 1)

1. Log in as **Ni Kang (Employee)** → select **Documents** in the navigation menu.
2. Choose a PDF under 25 MB, Title `Test Document`, Category `Personal Files`, select **Upload**.
   - Expect a progress indicator, then a success message; the document appears in **My Documents**
     with title, category, upload date, size, and type.
3. Try the 30 MB file → error mentions the 25 MB limit; nothing is added.
4. Try `fake.pdf` (renamed executable) → rejected by the scan; nothing is added.
5. Try uploading without a title → validation message; nothing is uploaded.
6. Upload with Project `ContosoDashboard Development` → project column shows it.
7. Upload 3 valid files at once → 3 documents listed.
8. Log out, log in as **Camille Nicole** → My Documents does not show Ni Kang's personal documents,
   and Notifications shows "New project document" for the project upload from step 6.

## Later stories (P2–P7)

- **P2**: Sort/filter My Documents, search by tag, download, preview a PDF and a PNG; request
  `/documents/{id}/download` for another user's personal document → `404`.
- **P3**: As Camille (PM) upload to the project; as Ni Kang see it on `/projects/1` and get a
  notification; as Camille delete a member's project document.
- **P4**: Edit metadata, replace file, delete with confirmation; another user cannot edit.
- **P5**: Share with Floris Kregel → notification and Shared with Me entry; share with department.
- **P6**: Upload from a task; dashboard shows Recent Documents (5) and the document count.
- **P7**: As System Administrator open `/documents/reports`; as Ni Kang access is denied.
