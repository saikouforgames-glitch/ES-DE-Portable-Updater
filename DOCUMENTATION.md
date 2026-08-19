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
| Auto-cleanup | After a successful update, the downloaded package (zip + extracted folder) is removed automatically; the **Delete Package** button cleans it up manually at any time |
| Progress logging | Text-based status log; live download progress bar with percent lines; robocopy output streamed live |
| Themes | System / Light / Dark |
| Portable settings | `settings.json` next to the exe; no registry or `%AppData%` |

---

## 3. Safety Principles

The updater's main goal is to **prevent accidental data loss**. Every feature prioritizes safety over convenience:

1. **Fail-closed validation** — any location or path that cannot be *proven* safe is refused with a clear explanation, whether picked or pasted. False rejection is always preferred over false acceptance.
2. **Hard location gates** — the destructive Current folder is refused when it *is* one of the protected locations itself: any drive root, Windows, Program Files (x86), ProgramData, the user profile root, `C:\Users`, `$Recycle.Bin`. Subfolders of these (e.g. `C:\Users\Leon\Downloads\ES-DE`, `C:\Program Files\ES-DE`) are accepted. The Package is refused only as a drive root.
3. **Physical identity checks** — the Current and Package folders must be two different physical folders (volume serial + file index, resolved through junctions, short names and drive aliases), and neither may contain the other — **except** a Package stored inside the updater's own preserved `ES-DE Updater` folder (where downloaded packages live), which never collides with the delete or copy steps.
4. **Detailed messages** explain *what was found*, *why it is a problem*, and *what to do next*.
5. **Optional backup** (off by default) creates a recovery point inside `Current\Backup` **before** anything is deleted.
6. **The preserved folders are never touched** — `Emulators`, `ROMs`, `Backup`, `ES-DE Updater`, and the user-data folder (`ES-DE` / `.emulationstation`) are excluded from both the delete and the copy steps.
7. **Old program files are deleted before copying** — no stale program files (including `resources`, `themes`, `ROMs_ALL`, etc.) remain after an update; the Package's own copies take their place.
8. **The data folder is renamed, never copied over** — and only when the target name differs and does not already exist.
9. **The updater never deletes itself** — the running-updater overlap rule allows updates for an updater stored inside a preserved folder (`ES-DE Updater`) and refuses any layout in which the sweep would delete it.
10. **Dogged re-verification** — the Current folder's physical identity is re-checked before the rename, the delete and the copy; if it changed mid-update (moved, replaced, rekindled), the update stops immediately.
11. **Running-program guard** — programs running from the destructive scope block the update; programs running from preserved folders do not.
12. **Executable rules match reality** — a Package must contain `ES-DE*.exe` (modern) or `EmulationStation*.exe` (older 2.x), so the strict rule accepts both eras; **`ES-DE Updater.exe` is excluded** so the updater cannot pass validation as the real ES-DE executable; the Current folder has no executable requirement at all — a damaged install is repaired, not refused.

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
| `btnAdvanced` | **Advanced…** — opens the Excluded Items window for the selected Current folder |
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

Defaults: 800×581 window (min 640×480), centered. Path fields, buttons, backup label, version labels, and log anchor to the window edges.

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

### 5.3 Advanced Window (Excluded Items)

Opened with **Advanced…** (requires a valid Current folder). Lists every top-level folder and file of the Current folder with a checkbox:

| State | Meaning |
|-------|---------|
| Required (checked, locked) | `Emulators`, `ROMs`, `ES-DE` / `.emulationstation`, `Backup`, `ES-DE Updater` and anything else always preserved — cannot be unchecked |
| Auto (🔒 checked, locked) | `portable.txt` redirect targets — set automatically when `portable.txt` points the data folder inside the Current folder |
| Kept (checked) | User-selected items — never deleted **and** never overwritten by the package copy |
| Deleted/replaced (unchecked) | Default state for everything else |
| “– no longer exists” | Restored from a previous session but missing in the current folder — greyed, not applied, removed by leaving it unchecked |

- The two data-folder rows (`.emulationstation` / `ES-DE`) show a greyed note explaining the generation-change behavior: *renamed to ES-DE when upgrading to 3.x* and *renamed to .emulationstation when downgrading to 2.x*.
- `BtnRestoreDefaults` — **Restore Defaults** unchecks every user-selected item (required and auto-protected items stay locked+checked) and turns **Remember excluded folders and files** back on. Applies to the exclusion list only; all other settings are untouched, and nothing is persisted until **OK**.

- `chkRememberExclusions` — **Remember excluded folders and files (across sessions)** (default on). When on, the list is restored at startup and validated against the remembered Current folder: names that no longer exist are dropped and reported in the log.
- Exclusions take effect immediately after OK and are shown in the update confirmation as **EXCLUDED — KEPT (NOT DELETED, NOT OVERWRITTEN)**.

---

## 6. Settings Reference

`settings.json` is stored beside the executable (`{AppContext.BaseDirectory}`).

```json
{
  "LastOldPath": "C:\\Current ES-DE",
  "LastNewPath": "C:\\Package",
  "RememberLastFolders": true,
  "RememberExclusions": true,
  "ExcludedTopLevelNames": [],
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
| `RememberExclusions` | Restore and validate excluded top-level items on startup (default `true`) |
| `ExcludedTopLevelNames` | Names kept during an update (never deleted, never overwritten); managed via **Advanced…** |
| `EnableBackup` | Master switch for the backup step (default `false`) |
| `BackupEmulators` | Back up `Emulators` (default `true`) |
| `BackupEsDe` | Back up the user-data folder (default `true`) |
| `BackupRoms` | Back up `ROMs` (default `true`) |
| `BackupRomsAll` | Also back up `ROMs_ALL` (default `false`) |
| `LastBackupLocation` | Path of the most recent backup (`{CurrentPath}\Backup`) for the **Delete Backup** button (default empty) |
| `AutoDeletePackage` | Remove the downloaded package (zip + extracted folder) automatically after a successful update (default `true`) |
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

- The updater finds the ES-DE executable (`ES-DE*.exe`, else the first `.exe`) in each folder, **skipping `ES-DE Updater.exe`**, and reads its `ProductVersion` from the shared Windows version resource. It reads metadata only — it does not execute the program.
- Comparison is **numeric component-by-component** (`Version.CompareTo`): `3.10.0` beats `3.9.0`.
- Results influence the UI and messages:

| Comparison | Direction | Start button |
|---|---|---|
| Package > Current | Upgrade | **Start Upgrade** |
| Package < Current | Downgrade | **Start Downgrade** |
| Equal or unreadable | Repair / Unknown | **Start Repair** |

- Version labels (`v3.4.1`) next to the folder fields update live, and the confirmation dialog shows a line such as `Detected: current 3.4.1 → package 3.2.0 (Downgrade)`.

### 7.3 Delete and copy

**Preserved scope** (never deleted, never copied over):

- `Emulators` — your emulators
- `ES-DE` / `.emulationstation` — user data
- `ROMs` — your games
- `Backup` / `ES-DE Updater` — the updater's own folders

**Excluded scope**: user-selected items from the **Advanced…** window are added to the preserved scope on top of the always-preserved folders — they are never deleted **and** never overwritten by the copy. Two exclusions are added automatically and locked when `portable.txt` redirects the data folder to a path *inside* the Current folder: `portable.txt` itself (the package's copy is not installed over it) and the top-level folder that contains the redirected data location. A redirect pointing *outside* the Current folder needs no sweep exclusion (the sweep only ever touches the Current folder) — `portable.txt` itself is still kept.

When `RememberExclusions` is on, the persisted list is restored at startup and validated against the remembered Current folder: names that no longer exist there are dropped and reported in the log.

**Replaced scope**: every other top-level item is **deleted** from Current and then re-created from the Package — program files, `resources`, `themes`, `licenses`, `es-pdf-converter` and **`ROMs_ALL`** (the package ships its own `ROMs_ALL` skeleton; by design the old one is replaced, like any other program-managed folder).

**Copy scope**: every remaining root directory/file from the Package (the `ES-DE.exe` / `EmulationStation.exe`, `resources`, `themes`, optionally `ROMs_ALL`, and anything else).

**Mechanics**: directories via robocopy `/E /Z /NFL /NDL /NJH /NJS /R:1 /W:1 /XJ` (no `/MIR`; one retry after a second — locked files fail fast; junctions are never followed); files via `File.Copy(overwrite)`. Missing source items are skipped with a warning; a robocopy failure (exit ≥ 8) aborts the update.

### 7.4 Data folder rename (v1.x/2.x vs 3.x)

ES-DE changed its portable data folder name at **version 3.0.0 (17 February 2024)**:

- 1.0.0 – 2.0.1 → `.emulationstation`
- 3.0.0 + → `ES-DE`

Any root folder named `ES-DE` or `.emulationstation` is treated as the user data folder. When the Current and Package folders use **different** data names, the Current data folder is **renamed** (after backup, before the delete) so the target version finds it:

- Current `ES-DE` → Package `.emulationstation` (downgrade)
- Current `.emulationstation` → Package `ES-DE` (upgrade)
- Same name → no change

Safety rules:

- **`portable.txt` redirects**: when `portable.txt` contains a path, that location is authoritative for the user data folder (the root folders `ES-DE` / `.emulationstation` are ignored for detection). The rename then happens at the redirected base path, and `portable.txt` + its target location are kept (see §7.3).
- If the target name already exists at the rename location, the update **fails closed** — it stops with an error explaining that both `ES-DE` and `.emulationstation` data folders were found and that the duplicate must be resolved manually. No silent skip, no overwriting.
- The confirmation dialog shows a **Data folder rename** section with version context (e.g. `Downgrade: this package is from before ES-DE 3.0.0 (before 17 February 2024)…`).
- The log records `→ Data folder rename: ES-DE → .emulationstation.` and `✔ Data folder renamed.`

### 7.5 Backup (off by default)

- Runs only when at least one backup folder is selected.
- Copies the selected data folders into `Current\Backup` **before** anything is deleted or renamed.
- Falling inside the Current install, the backup survives the removal of the Package folder.
- The location is stored in `LastBackupLocation` and can be removed with the **Delete Backup** button (permanent, no Recycle Bin).
- Failure messages point the user to `Current\Backup` as a recovery source.

### 7.6 Updater folder

`ES-DE Updater.exe` may live anywhere. The `ES-DE Updater` folder is never deleted from Current and never copied from the Package. The updater writes its downloads and extractions into `ES-DE Updater\packages\` next to the executable — this location is preserved, so a Package stored there passes the relationship check (it is never touched by the delete or copy steps). The folder ships inside the release ZIP; users unzip it onto the ES-DE root and their updater stays there on every upgrade, downgrade, and repair.

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
   - Full validation — any failure shows a detailed message and **nothing runs** (location gates, identity, structure, reversal, profiles, executable rules).
   - Running-program check — programs running from the destructive scope block with a list; preserved folders and system processes are ignored.
   - Build the copy list and show the **confirmation dialog**: paths, versions/direction, copy items, backup status, data-folder rename notice (if any), disk space summary.
   - Block if space is insufficient.
   - Capture the Current folder's identity seal.
   - Disable controls; clear the log.
   - Optional backup → `Current\Backup`.
   - Data folder rename (if needed) — seal re-checked.
   - Delete old program files (skip the preserved folders) — seal re-checked.
   - Copy items from the Package (robocopy for directories, `File.Copy` for files) — seal re-checked.
   - Save settings.
   - Show success (or error). `UpdateBackupUi` and `UpdateDirectionUi` refresh the Delete Backup button and version labels.

### Example log — backup off, downgrade

```
✔ Current ES-DE verified (v3.4.1).
✔ Package verified (v3.2.0).
↳ Running-program check: no program is running from the ES-DE folder.
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
↳ Running-program check: no program is running from the ES-DE folder.
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
⚠ ES-DE.exe NOT FOUND — REPAIR MODE. The package executable will be installed into the Current folder.
⚠ Could not delete resources\themes\some-file.ttf: The process cannot access the file because it is being used by another process.
⚠ Skipping readme.txt (not found in the package).
✖ Error: Robocopy failed while copying resources. Exit code: 16
⚠ Data folder rename skipped — \".emulationstation\" already exists in the current ES-DE folder.
```

---

## 9. Validation System

| Method | When | Checks |
|--------|------|--------|
| `ValidateOldFolder` | Browse (Current) | Location gate, path form, folder exists, structure presence (`Emulators`, `ROMs`, a user-data folder). **No executable requirement** — a damaged install (repair mode) is accepted |
| `ValidateNewFolder` | Browse (Package) | Location gate (drive root refused), folder exists, **strict executable rule** (`ES-DE*.exe` / `EmulationStation*.exe` / ES-DE version metadata); **`ES-DE Updater.exe` excluded** |
| `ValidateForUpdate` | Start | Everything below, including the location gates again (covers settings-restored and typed/programmatic paths) |

Validation steps (both folders filled, in order):

1. **Location gates** — Current refused only when it is itself a protected location (drive root, Windows, Program Files (x86), ProgramData, the user profile root, `C:\Users`, `$Recycle.Bin`) or for updater-overlap layouts the sweep would delete; subfolders of protected locations are accepted. Package refused only as a drive root.
2. **Different physical folders** — same folder (identity via volume serial + file index, resolved through junctions, 8.3 names and drive aliases) is refused; neither selection may contain the other — **except** a Package inside the updater's own preserved `ES-DE Updater` folder (`ES-DE Updater\packages\...`), which is never in the destructive scope.
3. Both folders exist.
4. **Package executable rule** — the Package must contain `ES-DE*.exe` (modern) or `EmulationStation*.exe` (older 2.x) or an exe whose version metadata identifies ES-DE; **`ES-DE Updater.exe` is excluded** (it is a tool, not ES-DE itself); `random.exe` is refused. **Current has no executable requirement** (repair mode).
5. Both contain the folders `Emulators` and `ROMs`.
6. Both contain a user data folder named `ES-DE` or `.emulationstation` (**empty is fine** — fresh packages have empty data folders). When the Current folder's `portable.txt` contains a path, that pointed-to location is used instead — no user data folder is required inside the root itself, and the error message names the referenced path when none is found there.
7. **Reversal detection** — a Current folder that looks fresh while Package looks populated ⇒ blocked with “Folders Appear Reversed”.
8. **Both-fresh detection** — both folders look fresh ⇒ Current must be the existing install.
9. **Current profile** — `Emulators` not empty, `ROMs` contains game files.
10. **Package profile** — `Emulators` empty, `ROMs` empty (each must look like a freshly extracted release).

The same gates apply at Start (`ValidateForUpdate` calls the location gates itself), so Browse-less paths — including paths restored from `settings.json` — cannot bypass the location checks. Browse dialogs are additionally box-aware: Current refusals are titled **Invalid Current ES-DE Folder**, Package refusals **Invalid Package Folder**, and a Current pick that lands on the data folder (`ES-DE` or `.emulationstation`) gets a hint pointing at the package root.

**ROM detection:** supported extensions are read from the installation's own `resources\systems\windows\es_systems.xml` (every `<extension>`), falling back to `resources\systems\es_systems.xml`, with a built-in fallback list when both files are missing or unreadable. `ROMs` is scanned recursively.

---

## 10. Expected Folder Layout

```
C:\Current ES-DE\          ← Existing installation (destination)
├── ES-DE.exe              ← fresh copy (restored in repair mode when missing)
├── Emulators\             ← preserved
├── ES-DE\ / .emulationstation\ ← preserved (renamed only when versions differ)
├── ROMs\                   ← preserved
├── ROMs_ALL\               ← replaced by the Package (by design)
├── resources\  themes\ …  ← fresh copies
├── Backup\ (optional)     ← the updater's backup
└── ES-DE Updater\ (optional) ← user-provided; preserved

C:\Package\                ← Extracted package (source)
├── ES-DE.exe / EmulationStation.exe ← copied from here (strict name rule)
├── Emulators\             ← NOT copied
├── ES-DE\ / .emulationstation\ ← NOT copied
├── ROMs\                  ← NOT copied
├── ROMs_ALL\ (optional)   ← copied (replaces Current's)
├── resources\             ← copied
└── themes\ …              ← copied
```

After an update the Current folder contains only fresh program files from the selected package plus the preserved data — no obsolete program files remain.

---

## 11. Error Handling

| Current or Package empty | Detailed message before the update starts |
| Path invalid, too long, or with quotes/wildcards/control chars | Refused with explanation (canonicalization) |
| Protected location itself (drive root, `C:\Windows`, Program Files, ProgramData, profile root, `C:\Users`, `$Recycle.Bin`) as Current | Blocked with a location-specific message — also at Start for typed/restored paths. Subfolders of these are accepted |
| Junction unresolved, link loop, or a folder link broken | Blocked — the updater will not run through a link it cannot verify |
| Update would delete the updater itself | Blocked by the updater-overlap rule |
| Programs running from the destructive scope | Blocked with a list; no Continue-anyway. Programs under preserved folders are ignored |
| Current folder identity changed mid-update | Aborts immediately — “The Current folder changed on disk after it was validated” |
| No ES-DE/EmulationStation executable in Package | Blocked on Browse and on update |
| Current without executable | Accepted as **repair mode** — banner + status show the warning; the executable is restored from the Package |
| No data folder (`ES-DE`/`.emulationstation`) | Blocked with names + era explanation |
| Same physical folder in both | Blocked (identity check — catches junctions, 8.3 names, drive aliases) |
| Reversed folders | Blocked with “Folders Appear Reversed” + comparison |
| Both fresh | Blocked with “Current must be the installation” |
| Current `Emulators` empty / no games | Blocked with explanation |
| Package already populated | Blocked (may have swapped selections) |
| Insufficient free space | Blocked before copy/backup |
| Deletion errors (locked/permissions) | Warning per item, continue |
| All program files fail to delete | Update aborted — cannot proceed safely |
| Missing source item | Warning, skip |
| Robocopy exit ≥ 8 | Stop, error |
| Robocopy locked file | Retries once (`/R:1 /W:1`) and fails in seconds |
| Robocopy hangs | Times out after 30 minutes; process killed |
| Version resource unreadable | Unknown version; falls back to Start Repair; still functional |
| Download stalls (no data for 90 s) | Fails with a clear message; partial files cleaned up |
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
├── AdvancedForm.cs
├── AdvancedForm.Designer.cs
├── AppSettings.cs
├── SettingsService.cs
├── ESDEUpdater.resx
├── ESDEUpdater.csproj
├── EsDeValidation.cs
├── ValidationResult.cs
├── FolderAnalysis.cs
├── FolderAnalyzer.cs
├── FolderNames.cs
├── EsDeVersionService.cs
├── SupportedRomExtensions.cs
├── PathSafety.cs
├── ValidationGate.cs
├── ProcessGuard.cs
├── Diagnostics.cs
├── RobocopyService.cs
├── ReleaseService.cs
├── BackupService.cs
├── DiskSpaceHelper.cs
├── DownloadManager.cs
├── UpdateOrchestrator.cs
├── ThemeService.cs
├── ThemedButton.cs
├── ThemedCheckBox.cs
├── ThemedProgressBar.cs
├── ThemedTextBox.cs
├── tests/ESDEUpdater.Tests/  (xunit suites for the non-UI services)
└── DOCUMENTATION.md            (this file)
```

---

## 13. Source File Reference

- `Program.cs` — Windows Forms entry point.
- `MainForm.cs` / `.Designer.cs` — main window and UI orchestration. Key members: `UpdateDirectionUi` (version labels + Start button text), `UpdateBackupUi` (backup state + Delete Backup), `UpdatePackageUi` (package cleanup + Delete Package), `EnsureAutoExclusions` (portable.txt protection), and the running-program gate (`ProcessGuard`). Re-entrancy guard `_updateRunning`. The update pipeline and the download flow are delegated to `UpdateOrchestrator` / `DownloadManager`.
- `UpdateOrchestrator.cs` — the update pipeline, no UI dependencies (status via callback): `BuildUpdatePlan` (copy list, backup-folder selection, disk-space check, data-folder rename detection), `BuildConfirmationMessage` (preview with repair-mode banner and EXCLUDED sections), and `ExecuteUpdateAsync` (backup → fail-closed data-folder rename → seal re-verified delete sweep → seal re-verified copy). Also defines `UpdateDirection`, `UpdatePlan`, and `OldFolderSeal`.
- `DownloadManager.cs` — the GitLab download flow, no UI dependencies: `FetchLatestReleaseAsync`, `BuildDownloadConfirmation` (outdated / up-to-date / no-current states), and `ExecuteDownloadAsync` (download → MD5 verify → smart extract → package validation, with corrupt-download cleanup).
- `SettingsForm.cs` / `.Designer.cs` — settings dialog.
- `AdvancedForm.cs` / `.Designer.cs` — excluded-items dialog for the selected Current folder (checkbox list, Restore Defaults, remember toggle).
- `AppSettings.cs` — settings model (section 6).
- `SettingsService.cs` — JSON load/save beside the exe.
- `EsDeValidation.cs` / `ValidationResult.cs` / `FolderAnalysis.cs` / `FolderAnalyzer.cs` — location gates, structural validation, reversal/profile checks, and folder analysis (executable presence, data-folder name, emulator/ROM counts). Browse is box-aware (per-field messages), Start re-runs the location gates for typed/restored paths.
- `FolderNames.cs` — single source of the preserved-folder names (`Emulators`, `ES-DE`, `.emulationstation`, `ROMs`, `Backup`, `ES-DE Updater`), the `RomsAll` constant, and the two known data-folder names.
- `EsDeVersionService.cs` — `FindExecutable`, `TryGetDisplayVersion` (ProductVersion, FileVersion), `TryParse`.
- `SupportedRomExtensions.cs` — `es_systems.xml` + fallback extension lists.
- `PathSafety.cs` — fail-closed path canonicalization: rejects unsafe forms (quotes/wildcards/control chars/UNC/GLOBALROOT), expands 8.3 names, resolves every reparse point manually, and exposes `DirectoryIdentity` (volume serial + file index) for physical-equality checks.
- `ValidationGate.cs` — hard location rules: protected-area refusals for Current, drive-root refusal for Package, same-physical-folder and mutual-nesting refusals, and the updater-overlap rule (updater under a preserved name is fine; any layout the sweep would delete is refused).
- `ProcessGuard.cs` — running-program scan limited to the destructive scope; preserved folders and uninspectable system processes are ignored.
- `Diagnostics.cs` — injectable status-log sink used by all services.
- `RobocopyService.cs` — `robocopy /E /Z /NFL /NDL /NJH /NJS /R:1 /W:1 /XJ` with a 30-minute timeout (exit codes 0–7 are success, ≥8 is failure; locked files fail fast, junctions are never followed).
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
6. Executable rules differ by role: the **Package** requires a strict ES-DE executable (`ES-DE*.exe`, `EmulationStation*.exe`, or ES-DE version metadata) — **`ES-DE Updater.exe` is excluded** so the updater cannot masquerade as ES-DE — while the **Current** folder has no executable requirement — missing/damaged executables are repaired, not refused.
7. Everything not in the preserved list is **replaced, not skipped** — including `ROMs_ALL` and other program-managed folders (matches how official ES-DE packages are structured).
8. Backup default off, stored inside the install, one-click delete.
9. Validation uses real structure, not folder names alone.
10. Same-folder detection is **physical** (volume serial + file index), not string-based — junctions, 8.3 names, casing and subst drives cannot fake it.
11. Fail-closed path handling: any path that cannot be canonicalized through every link is refused rather than handled by guesswork.
12. ROM extensions follow the installed `es_systems.xml` with a fallback.
13. Settings beside exe — USB-friendly, machine-independent.
14. Simple rename instead of copy/migrate; the status log is kept deliberately simple.
15. Asynchronous run keeps the UI responsive.
16. Running-updater overlap rule: the sweep may never delete the running updater — allowed only when the updater sits under a preserved name (`ES-DE Updater`).
17. The folder seal re-verifies identity before every destructive step — an update can never write into a folder that was swapped mid-flight.
18. Official release info consumed from GitLab (API + `latest_release.json`) — no scraping, no third-party mirrors; prereleases skipped by tag name.

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
7. **Cleanup** — after a successful update, if `AutoDeletePackage` is on (default) the ZIP + extraction container (including the inner `ES-DE` root) are removed automatically and the Package path is cleared. The **Delete Package** button can also remove the downloaded package at any time (useful before running the upgrade or for early cleanup). Failures and cancelled downloads never delete anything.

### 21.3 Notes

- Uses `HttpClient` (`.NET 8` framework, no NuGet packages) and `System.IO.Compression.ZipFile` for extraction.
- Download progress is shown live on a progress bar above the status log, with a `→ Downloading... N%` line appended to the log every 10% (start / checksum / extraction lines also appear). If the server does not report the file size, the bar switches to an indeterminate animation.
- If no data is received for **90 seconds**, the download fails with a clear message ("Download stalled…"); interrupted or failed downloads are cleaned up automatically.
- Extraction handles both ZIP layouts: files at the ZIP root, or a single wrapping folder (unwrapped automatically).
- If the ZIP was extracted with a single root folder, that folder is used as the package root; otherwise the extraction directory itself.
- The **Delete Package** button only ever removes the app's own tracked download paths (`LastPackageZip` / `LastPackageExtracted` container) — a manually-browsed package is never deleted.