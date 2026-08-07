# ES-DE Portable Updater — Documentation

## 1. Overview

**ES-DE Portable Updater** is a standalone Windows utility that refreshes a portable [EmulationStation Desktop Edition (ES-DE)](https://es-de.org/) installation with program files copied from another ES-DE package. It supports **upgrades**, **downgrades**, and **same-version repairs** of a portable (non-installer) ES-DE folder.

Update direction is detected automatically by reading the **product version** embedded in each ES-DE executable, and the UI adapts (Start Upgrade / Start Downgrade / Start Repair). When moving between ES-DE versions that use a different user-data folder name (`ES-DE` vs `.emulationstation`), the updater renames the data folder so the target version finds your settings.

An optional **"Download Latest"** feature pulls the latest stable release directly from the official ES-DE GitLab repository, compares it against your installed version, downloads the Windows x64 portable ZIP, verifies its MD5 checksum, extracts it, and sets it as the Package. Your ES-DE can then be kept always up to date with one button.

The operation is a **clean program refresh**:

- All existing program files are deleted from the installation.
- Fresh copies are brought in from the selected package.
- The large personal data folders (`Emulators`, `ROMs`, and the user-data folder `ES-DE` / `.emulationstation`) are **preserved and never copied or deleted**.
- The data folder may be *renamed* only when the two versions use different folder names (see section 7.4).

The application is fully portable:

- No installer required, no Windows Registry usage
- Can run from a USB drive or any folder
- Settings stored in a `settings.json` file beside the executable

---

## 2. Feature Summary

| Feature | Behavior |
|---------|----------|
| Version detection | Reads the ES-DE executable's product version from its Windows version resource and compares Current vs Package numerically |
| Upgrade / Downgrade / Repair | Direction detected automatically; the Start button shows **Start Upgrade**, **Start Downgrade**, or **Start Repair** |
| Data folder rename | Renames the user data folder (`ES-DE` ↔ `.emulationstation`) when Current and Package use different names |
| Clean program refresh | Program files deleted first, then fresh copies installed from the selected package; data folders preserved |
| Folder validation | Detects swapped Current/Package selections, fresh extracts, missing executables, and missing data folders |
| Optional backup | **Off by default**; when enabled, backs up selected data folders into `Current\Backup` before copying |
| Delete Backup | The remembered backup location can be deleted with one button |
| Disk space check | Estimates copy/backup space needs and blocks the update if there is not enough free space |
| Download Latest | Fetches the latest stable release from the official GitLab repo, compares versions, downloads the Windows x64 portable ZIP, verifies MD5, extracts it, and sets it as the Package |
| Auto-cleanup | After a successful upgrade, the downloaded package (zip + extracted folder) is removed automatically; the **Delete Package** button cleans it up manually at any time |
| Progress logging | Text-based status log; live download progress bar with percent lines; robocopy output streamed live |
| Themes | System / Light / Dark |
| Portable settings | `settings.json` next to the exe; no registry or `%AppData%` |

---

## 3. Safety Principles

The updater's main goal is to **prevent accidental data loss**. Every feature prioritizes safety over convenience:

1. **Smart validation** detects swapped folders, fresh extracts, and invalid folders before anything runs — and blocks with a clear explanation.
2. **Detailed messages** explain *what was found*, *why it is a problem*, and *what to do next*.
3. **Optional backup** (off by default) creates a recovery point inside `Current\Backup` **before** anything is deleted.
4. **The data folders are never touched** — `Emulators`, `ROMs`, and the user-data folder (`ES-DE` / `.emulationstation`) are excluded from both the delete and the copy steps.
5. **Old program files are deleted before copying** — no obsolete files remain after an update.
6. **The data folder is renamed, never copied over** — and only when the target name is different and does not already exist.

---

## 4. Quick Start

1. Run **ES-DE Updater.exe**.
2. **Browse** → **Current ES-DE (In Use)** — select your existing installation.
3. *(Recommended)* Click **Download Latest** to fetch and extract the newest official ES-DE release as the Package automatically, or **Browse** → **Upgrade/Downgrade Package** to select an extracted package manually.
4. The version labels and the Start button update automatically with the detected direction (Upgrade / Downgrade / Repair).
5. *(Optional)* **Settings** → **Enable Backup** (+ choose folders) for a recovery point.
6. Click **Start Upgrade / Start Downgrade / Start Repair** and confirm.
7. The updater validates, backs up (if enabled), renames the data folder if needed, deletes old program files, and installs the selected version.
8. Launch ES-DE from your **Current** installation — it is now the version you selected.
9. *(Optional)* If a backup exists, the **Delete Backup** button removes it.

---

## 5. User Interface

### 5.1 Main Window

| Control | Purpose |
|---------|---------|
| `lblTitle` | Title: **ES-DE Portable Updater** |
| `btnSettings` | Opens the Settings window |
| `lblOldFolder` / `txtOldFolder` | **Current ES-DE (In Use)** — the installation being refreshed |
| `lblOldVersion` | Detected version of the Current folder (e.g. `v3.4.1`); blank when unreadable |
| `btnBrowseOld` | Folder picker for the Current folder |
| `lblNewFolder` / `txtNewFolder` | **Upgrade/Downgrade Package** — the source |
| `lblNewVersion` | Detected version of the Package folder; blank when unreadable |
| `btnBrowseNew` | Folder picker for the Package folder |
| `btnStartUpdate` | Starts the operation. Text reflects direction: **Start Upgrade** / **Start Downgrade** / **Start Repair** |
| `btnDownloadLatest` | **Download Latest** — checks GitLab for the newest stable release, compares with the installed version, downloads/extracts/verifies the Windows x64 portable ZIP, and sets it as the Package |
| `btnDeletePackage` | **Delete Package** — enabled when a downloaded package (zip and/or extracted folder) exists; removes it after confirmation |
| `lblBackupStatus` | Backup state, e.g. **Backup: Off** or **Backup: On — C:\Backup (Emulators, ES-DE, ROMs)** |
| `btnDeleteBackup` | Enabled when a backup exists at the remembered location; deletes it after confirmation |
| `txtStatusLog` | Multiline read-only log (Consolas font) |

Defaults: 800×564 window (min 640×480), centered. Path fields, buttons, backup label, version labels, and log anchor to the window edges.

### 5.2 Settings Window

| Group | Control | Purpose |
|-------|---------|---------|
| General | `chkRememberFolders` | Restore/save the last Current and Package paths (default on) |
| General | `chkAutoDeletePackage` | Auto-delete the downloaded package after a successful upgrade (default on) |
| Backup Options | `chkEnableBackup` | Master switch for the backup step (default off) |
| Backup Options | `chkBackupEmulators` | Back up `Emulators` (default on) |
| Backup Options | `chkBackupEsDe` | Back up the user-data folder `ES-DE`/`.emulationstation` (default on) |
| Backup Options | `chkBackupRoms` | Back up `ROMs` (default on) |
| Backup Options | `chkBackupRomsAll` | Back up `ROMs_ALL` (default off) |
| Appearance | `rdoThemeSystem` / `rdoThemeLight` / `rdoThemeDark` | Theme (default System) |
| Advanced | `btnRestoreDefaults` | Reset all settings |
| — | `btnSave` / `btnCancel` | Save / discard |

The four backup checkboxes are greyed out when `chkEnableBackup` is unchecked.

---

## 6. Settings Reference

`settings.json` is stored beside the executable (`{AppContext.BaseDirectory}`).

```json
{
  "LastOldPath": "C:\\Current ES-DE",
  "LastNewPath": "C:\\Package",
  "RememberLastFolders": true,
  "EnableBackup": false,
  "BackupEmulators": true,
  "BackupEsDe": true,
  "BackupRoms": true,
  "BackupRomsAll": false,
  "LastBackupLocation": "",
  "AutoDeletePackage": true,
  "LastPackageZip": "",
  "LastPackageExtracted": "",
  "Theme": "System"
}
```

| Property | Description |
|----------|-------------|
| `LastOldPath` | Last used Current ES-DE folder |
| `LastNewPath` | Last used Package folder |
| `RememberLastFolders` | Restore last paths on startup (default `true`) |
| `EnableBackup` | Master switch for the backup step (default `false`) |
| `BackupEmulators` | Back up `Emulators` (default `true`) |
| `BackupEsDe` | Back up the user-data folder (default `true`) |
| `BackupRoms` | Back up `ROMs` (default `true`) |
| `BackupRomsAll` | Also back up `ROMs_ALL` (default `false`) |
| `LastBackupLocation` | Path of the most recent backup (`{CurrentPath}\Backup`) for the **Delete Backup** button (default empty) |
| `AutoDeletePackage` | Remove the downloaded package (zip + extracted folder) automatically after a successful upgrade (default `true`) |
| `LastPackageZip` | Path of the most recently downloaded ZIP (for the **Delete Package** button / auto-cleanup) |
| `LastPackageExtracted` | Path of the extracted package folder (for the **Delete Package** button / auto-cleanup) |
| `Theme` | `System`, `Light`, or `Dark` (default `System`) |

Load: returns empty settings if the file is missing or corrupt; unknown JSON keys are ignored.  
Save: rewrites the file on browse, settings save, or completed update.

---

## 7. How an Update Works

### 7.1 Folders

| Folder | Role |
|--------|------|
| **Current** | Existing installation, the destination. Program files are deleted and replaced with the package's files; data is preserved. |
| **Package** | Extracted ES-DE folder, the source. Read-only from the updater's point of view. |

### 7.2 Version detection

- The updater finds the ES-DE executable (`ES-DE*.exe`, else the first `.exe`) in each folder and reads its `ProductVersion` from the shared Windows version resource. It reads metadata only — it does not execute the program.
- Comparison is **numeric component-by-component** (`Version.CompareTo`): `3.10.0` beats `3.9.0`.
- Results influence the UI and messages:

| Comparison | Direction | Start button |
|---|---|---|
| Package > Current | Upgrade | **Start Upgrade** |
| Package < Current | Downgrade | **Start Downgrade** |
| Equal or unreadable | Repair / Unknown | **Start Repair** |

- Version labels (`v3.4.1`) next to the folder fields update live, and the confirmation dialog shows a line such as `Detected: current 3.4.1 → package 3.2.0 (Downgrade)`.

### 7.3 Delete and copy

**Delete scope** (anything not in this list is deleted):

- `Emulators` — your emulators
- `ES-DE` / `.emulationstation` — user data
- `ROMs` — your games
- `Backup` / `ES-DE Updater` — the updater's own folders

**Copy scope**: every remaining root directory/file from the Package (the `ES-DE.exe`, `resources`, optionally `ROMs_ALL`, and anything else). Data folders are never copied over the existing ones, and never hidden by the delete step.

**Mechanics**: directories via robocopy `/E /Z` (no `/MIR`); files via `File.Copy(overwrite)`. Missing items are skipped with a warning; robocopy failure (exit ≥ 8) aborts.

### 7.4 Data folder rename (v1.x/2.x vs 3.x)

ES-DE changed its portable data folder name at **version 3.0.0 (17 February 2024)**:

- 1.0.0 – 2.0.1 → `.emulationstation`
- 3.0.0 + → `ES-DE`

Any root folder named `ES-DE` or `.emulationstation` is treated as the user data folder. When the Current and Package folders use **different** data names, the Current data folder is **renamed** (after backup, before the delete) so the target version finds it:

- Current `ES-DE` → Package `.emulationstation` (downgrade)
- Current `.emulationstation` → Package `ES-DE` (upgrade)
- Same name → no change

Safety rules:

- If the target name already exists in Current, the rename is **skipped** and a note is logged — no overwriting.
- The confirmation dialog shows a **Data folder rename** section with version context (e.g. `Downgrade: this package is from before ES-DE 3.0.0 (before 17 February 2024)…`).
- The log records `→ Data folder rename: ES-DE → .emulationstation.` and `✔ Data folder renamed.`

### 7.5 Backup (off by default)

- Runs only when at least one backup folder is selected.
- Copies the selected data folders into `Current\Backup` **before** anything is deleted or renamed.
- Falling inside the Current install, the backup survives the removal of the Package folder.
- The location is stored in `LastBackupLocation` and can be removed with the **Delete Backup** button (permanent, no Recycle Bin).
- Failure messages point the user to `Current\Backup` as a recovery source.

### 7.6 Updater folder

`ES-DE Updater.exe` may live anywhere. The `ES-DE Updater` folder is never deleted from Current and never copied from the Package — the updater itself never creates or writes to it. The folder ships inside the release ZIP; users unzip it onto the ES-DE root and their updater stays there on every upgrade, downgrade, and repair.

### 7.7 Disk space check

Shown in the confirmation dialog:

- **Copy size** — the program files to install onto the Current drive.
- **Backup size** — only when enabled (also on the Current drive).
- A safety margin (max 256 MB or 5 %) is added.
- If free space is insufficient, the update is blocked with the summary.
- With backup off, the summary says “Backup is disabled — no additional space required.”

---

## 8. Update Workflow (Step by Step)

1. **Browse / select Current** then Package. Validation runs, settings saved.
2. The **Start** button reflects the detected direction.
3. **Start:**
   - Refresh version/direction.
   - Full validation — any failure shows a detailed message and **nothing runs**.
   - Build the copy list and show the **confirmation dialog**: paths, versions/direction, copy items, backup status, data-folder rename notice (if any), disk space summary.
   - Block if space is insufficient.
   - Disable controls; clear the log.
   - Optional backup → `Current\Backup`.
   - Data folder rename (if needed).
   - Delete old program files (skip the preserved folders).
   - Copy items from the Package (robocopy for directories, `File.Copy` for files).
   - Save settings.
   - Show success (or error). `UpdateBackupUi` and `UpdateDirectionUi` refresh the Delete Backup button and version labels.

### Example log — backup off, downgrade

```
✔ Current ES-DE verified (v3.4.1).
✔ Package verified (v3.2.0).
→ Downgrade detected: 3.4.1 → 3.2.0.
⚠ Backup disabled — no backup created.
→ Data folder rename: ES-DE → .emulationstation.
✔ Data folder renamed.
Deleting resources...
✔ Deleted resources.
Deleting ES-DE.exe...
✔ Deleted ES-DE.exe.
Copying ES-DE.exe...
✔ Copying ES-DE.exe completed.
Copying resources...
✔ Copying resources completed.
✔ Finished.
```

### Example log status — backup on (Emulators, ES-DE, ROMs)

```
Current ES-DE verified (v3.4.1).
Package verified (v3.0.0).
→ Downgrade detected: 3.4.1 → 3.0.0.
Creating backup of Emulators...
✔ Backup of Emulators completed.
Creating backup of ES-DE...
✔ Backup of ES-DE completed.
Creating backup of ROMs...
✔ Backup of ROMs completed.
✔ Backup created.
→ Data folder rename: ES-DE → .emulationstation.
Deleting resources...
✔ Deleted resources.
Copying ES-DE.exe...
✔ Copying ES-DE.exe completed.
Copying resources...
✔ Copying resources completed.
✔ Finished.
```

### Example warnings

```
⚠ Could not delete resources\themes\some-file.ttf: The process cannot access the file because it is being used by another process.
⚠ Skipping readme.txt (not found in the package).
✖ Error: Robocopy failed while copying resources. Exit code: 16
⚠ Data folder rename skipped — ".emulationstation" already exists in the current ES-DE folder.
```

---

## 9. Validation System

| Method | When | Checks |
|--------|------|--------|
| `ValidateOldFolder` | Browse (Current) | Non-empty path, folder exists, a root `.exe` present |
| `ValidateNewFolder` | Browse (Package) | Same quick checks |
| `ValidateForUpdate` | Start | Full structural validation |

Validation steps (both folders filled):

1. Current and Package must differ.
2. Both folders exist.
3. Both contain a root executable (`.exe`) — any name.
4. Both contain the exact folders `Emulators` and `ROMs`.
5. Both contain a user data folder named `ES-DE` or `.emulationstation` (**empty is fine** — fresh packages have empty data folders).
6. **Reversal detection**: a Current folder that looks fresh while Package looks populated ⇒ blocked with “Folders Appear Reversed”.
7. **Both-fresh detection**: both folders fresh ⇒ Current must be the existing install.
8. **Current profile**: `Emulators` not empty, `ROMs` contains game files.
9. **Package profile**: `Emulators` empty, `ROMs` empty.

The data folder check is purely structural (a recognized name present). Reversal and profile checks use `ROMs` file counts and `Emulators` subfolder counts; the data check never weakens them.

**ROM detection:** supported extensions are read from the installation's own `resources\systems\windows\es_systems.xml` (every `<extension>`), with a built-in fallback list when the file is missing or unreadable. `ROMs` is scanned recursively.

---

## 10. Expected Folder Layout

```
C:\Current ES-DE\          ← Existing installation (destination)
├── ES-DE.exe / <any .exe>    ← fresh copy from the Package
├── Emulators\             ← preserved
├── ES-DE\ / .emulationstation\ ← preserved (renamed only when versions differ)
├── ROMs\                   ← preserved
├── resources\             ← fresh copy
├── Backup\ (optional)     ← the updater's backup
└── ES-DE Updater\ (optional) ← user-provided; preserved

C:\Package\                ← Extracted package (source)
├── <any .exe>               ← copied from here
├── Emulators\             ← NOT copied
├── ES-DE\ / .emulationstation\ ← NOT copied
├── ROMs\                  ← NOT copied
├── resources\             ← copied
└── ROMs_ALL\ (optional)   ← copied (new system folders)
```

After an update the Current folder contains only fresh program files from the selected package plus the preserved data — no obsolete program files remain.

---

## 11. Error Handling

| Scenario | Behavior |
|----------|----------|
| Current or Package empty | Detailed message before the update starts |
| No root executable (`.exe`) | Warning on browse; error on update |
| No data folder (`ES-DE`/`.emulationstation`) | Blocked with names + era explanation |
| Same folder in both | Blocked |
| Reversed folders | Blocked with “Folders Appear Reversed” + comparison |
| Both fresh | Blocked with “Current must be the installation” |
| Current `Emulators` empty / no games | Blocked with explanation |
| Package already populated | Blocked (may have swapped selections) |
| Insufficient free space | Blocked before copy/backup |
| Deletion errors (locked/permissions) | Warning per item, continue |
| All program files fail to delete | Update aborted — cannot proceed safely |
| Missing source item | Warning, skip |
| Robocopy exit ≥ 8 | Stop, error |
| Robocopy hangs | Times out after 30 minutes; process killed |
| Version resource unreadable | Unknown version; falls back to Start Repair; still functional |
| User cancels picker/confirmation | Nothing changes |
| Corrupt `settings.json` | Returns empty defaults |
| Window closed during update | Blocked by confirmation dialog |

The error dialog reminds the user that a backup may exist in `Current\Backup\` if the backup step was enabled and completed.

---

## 12. Project Structure

```
ESDEUpdater/
├── Program.cs
├── MainForm.cs
├── MainForm.Designer.cs
├── SettingsForm.cs
├── SettingsForm.Designer.cs
├── AppSettings.cs
├── SettingsService.cs
├── ESDEUpdater.resx
├── ESDEUpdater.csproj
├── EsDeValidation.cs
├── ValidationResult.cs
├── FolderAnalysis.cs
├── FolderAnalyzer.cs
├── EsDeVersionService.cs
├── SupportedRomExtensions.cs
├── RobocopyService.cs
├── ReleaseService.cs
├── BackupService.cs
├── DiskSpaceHelper.cs
├── ThemeService.cs
├── ThemedButton.cs
├── ThemedCheckBox.cs
├── ThemedProgressBar.cs
├── ThemedTextBox.cs
└── DOCUMENTATION.md            (this file)
```

---

## 13. Source File Reference

- `Program.cs` — Windows Forms entry point.
- `MainForm.cs` / `.Designer.cs` — main window and orchestration. Key members: `UpdateDirectionUi` (version labels + Start button text), `UpdateBackupUi` (backup state + Delete Backup), `UpdatePackageUi` (package cleanup + Delete Package), `BuildCopyItemList`, `BuildBackupFolderList`, `DeleteOldProgramFiles`, and the data-folder rename step. Re-entrancy guard `_updateRunning`.
- `SettingsForm.cs` / `.Designer.cs` — settings dialog.
- `AppSettings.cs` — settings model (section 6).
- `SettingsService.cs` — JSON load/save beside the exe.
- `EsDeValidation.cs` / `ValidationResult.cs` / `FolderAnalysis.cs` / `FolderAnalyzer.cs` — structural validation, reversal/profile checks, and folder analysis (executable presence, data-folder name, emulator/ROM counts).
- `EsDeVersionService.cs` — `FindExecutable`, `TryGetDisplayVersion` (ProductVersion, FileVersion), `TryParse`.
- `SupportedRomExtensions.cs` — `es_systems.xml` + fallback extension lists.
- `RobocopyService.cs` — `robocopy /E/Z /NFL/NDL/NJH/NJS` with a 30-minute timeout (exit codes 0–7 are success, ≥8 is failure).
- `ReleaseService.cs` — GitLab release lookup (`GetLatestReleaseAsync`), streaming `DownloadAsync`, `VerifyMd5`, `ExtractPackage` (with single-root unwrap). Data class `EsDeReleaseInfo`.
- `BackupService.cs` — `CreateBackupAsync`, `CheckSpace` (with margin), plus a formatted summary.
- `DiskSpaceHelper.cs` — free space / sizes.
- `ThemeService.cs` — System / Light / Dark styles recursively.

---

## 14. Technology Stack

| Component | Choice |
|-----------|--------|
| Language | C# |
| Framework | .NET 8 LTS (`net8.0-windows`) |
| UI | Windows Forms |
| File copy | Robocopy |
| Version source | Windows version resource (`FileVersionInfo`) |
| Update source | GitLab Releases API + `latest_release.json` (official ES-DE repo) |
| Archive | `System.IO.Compression.ZipFile` |
| Storage | `settings.json` beside exe |
| Output | `ES-DE Updater.exe` |

---

## 15. Building and Publishing

Prerequisite: .NET 8 SDK, Windows.

```powershell
# Debug/Release
dotnet build ESDEUpdater.csproj -c Release

# Self-contained single file
dotnet publish ESDEUpdater.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### Release packaging

The published single-file exe is packaged into a ZIP for distribution:

```
ES-DE Updater/          ← top-level folder
├── ES-DE Updater.exe   ← self-contained single-file exe
├── readme.txt          ← quick-start install instructions
└── DOCUMENTATION.md    ← full documentation
```

Steps:

1. `dotnet publish` (above) produces `ES-DE Updater.exe` in the publish output directory.
2. Create a staging folder `ES-DE Updater/` and copy the exe into it.
3. Add `readme.txt` (install instructions) and `DOCUMENTATION.md`.
4. Zip the `ES-DE Updater/` folder → `ES-DE-Updater-v1.0.0.zip`.
5. Attach the ZIP to a GitHub release.

---

## 16. Runtime Files

| File/Dir | Created | Purpose |
|----------|---------|---------|
| `settings.json` beside exe | First browse / settings / update | Stores last paths, backup options, theme, last backup location |
| `Current\Backup\` | When backup enabled | Recovery copy of data folders |
| `Current\ES-DE Updater\` | Never (user-provided) | preserved; never touched by the app |

---

## 17. Design Decisions

1. Robocopy over pure C# copy (restartable, efficient large trees).
2. Delete-before-copy eliminates stale program files.
3. Version-driven direction via `ProductVersion`, numeric comparison.
4. Data folder identified by the two known names; rename only when names differ and target absent.
5. Data-folder validation is **name-based**, never content-required (fresh packages are empty).
6. `.exe` check is flexible — any executable in the root works.
7. Backup default off, stored inside the install, one-click delete.
8. Validation uses real structure, not folder names alone.
9. ROM extensions follow the installed `es_systems.xml` with a fallback.
10. Settings beside exe — USB-friendly, machine-independent.
11. Simple rename instead of copy/migrate; the status log is kept deliberately simple.
12. Asynchronous run keeps the UI responsive.
13. Official release info consumed from GitLab (API + `latest_release.json`) — no scraping, no third-party mirrors; prereleases skipped by tag name.

---

## 18. Known Limitations

- Windows only (Robocopy).
- No dry-run preview, no cancel once running (except robocopy failure).
- Missing/rogue junk items are skipped with a warning, not fatal.
- Backup doubles disk usage while retained.
- Delete Backup removes folder permanently (no Recycle Bin).
- If the version resource is unreadable, direction falls back to Start Repair; no date-based comparison.
- **Download Latest** requires an internet connection to GitLab; interrupted or failed downloads are automatically cleaned up. Auto-delete only removes a package that was already used in a successful upgrade — nothing is deleted on failure or cancel.

---

## 19. Planned Future Improvements

- Dry-run preview
- Update-history log file
- Restore-from-backup workflow
- Timestamp-based fallback comparison when a version cannot be read

---

## 20. Version Compatibility Map

| | Data folder | Date |
|---|---|---|
| ES-DE 1.0.x – 2.0.1 | `.emulationstation` | before 3.0.0 (before 17 Feb 2024) |
| ES-DE 3.0.0+ | `ES-DE` | 3.0.0 (17 Feb 2024) onward |

- Both names are accepted as the user data folder.
- The updater renames between the two eras when required (section 7.4).
- `Emulators`/`ROMs` exist in every supported release and are always validated by exact name.

---

## 21. Download Latest (official GitLab source)

The **Download Latest** button keeps ES-DE always up to date by pulling the newest official release straight from the ES-DE GitLab repository. No manual download or extraction is needed.

### 21.1 Sources

- **Release list API** — `https://gitlab.com/api/v4/projects/es-de%2Femulationstation-de/releases?per_page=8` (newest first; prerelease tags like `alpha`/`beta`/`rc` are skipped).
- **Windows x64 portable asset** — picked from `assets.links` by name: `ES-DE_<ver>-x64_Portable.zip` (older 2.x releases: `EmulationStation-DE-<ver>-x64_portable.zip`).
- **MD5 checksum** — from `https://gitlab.com/es-de/emulationstation-de/-/raw/master/latest_release.json` (`stable.packages` → `WindowsPortable`); used when its `stable.version` matches the release found via the API.

### 21.2 Flow

1. Click **Download Latest**.
2. `ReleaseService.GetLatestReleaseAsync()` fetches the release list, finds the newest stable tag and its Windows portable asset, and (when available) the official MD5.
3. Versions are compared with `EsDeVersionService.TryParse`:
   - Newer available → **"A new version is available: vX → vY"** confirm dialog.
   - Already up to date → **"Already up to date"** dialog that still offers **Download anyway** (useful for repair/reinstall).
   - No Current folder set → simple "download as package" dialog.
4. On **Yes**: the ZIP streams to `packages\` beside the updater exe (`AppContext.BaseDirectory\packages`, created on demand), MD5 is verified when the official checksum is available (a mismatch asks before continuing; if no checksum could be fetched, verification is skipped with a note in the log), and the ZIP is extracted to `ES-DE-<version>-extract\`.
5. The extracted package is validated with `EsDeValidation.ValidateNewFolder`; the validated ES-DE root is then set as the **Upgrade/Downgrade Package** path (`txtNewFolder`), the direction UI refreshes, the ZIP + extraction-container paths are recorded (`LastPackageZip`, `LastPackageExtracted` — the latter is the `ES-DE-<version>-extract\` container so cleanup removes everything), and settings are saved.
6. The user confirms and presses **Start Upgrade** — the normal update flow takes over.
7. **Cleanup** — after a successful upgrade, if `AutoDeletePackage` is on (default) the ZIP + extraction container (including the inner `ES-DE` root) are removed automatically and the Package path is cleared. The **Delete Package** button can also remove the downloaded package at any time (useful before running the upgrade or for early cleanup). Failures and cancelled downloads never delete anything.

### 21.3 Notes

- Uses `HttpClient` (`.NET 8` framework, no NuGet packages) and `System.IO.Compression.ZipFile` for extraction.
- Download progress is shown live on a progress bar above the status log, with a `→ Downloading... N%` line appended to the log every 10% (start / checksum / extraction lines also appear). If the server does not report the file size, the bar switches to an indeterminate animation.
- Extraction handles both ZIP layouts: files at the ZIP root, or a single wrapping folder (unwrapped automatically).
- If the ZIP was extracted with a single root folder, that folder is used as the package root; otherwise the extraction directory itself.
- The **Delete Package** button only ever removes the app's own tracked download paths (`LastPackageZip` / `LastPackageExtracted` container) — a manually-browsed package is never deleted.