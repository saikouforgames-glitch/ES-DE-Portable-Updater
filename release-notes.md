## ES-DE Portable Updater v1.1.1

A standalone Windows utility that refreshes a portable [ES-DE](https://es-de.org/) installation — upgrades, downgrades, and same-version repairs — while preserving your games, emulators, and settings.

> **Windows only** — uses Robocopy and Windows Forms (`net8.0-windows`). Requires 64-bit Windows; does not run on Linux or macOS.

### What's New in v1.1.1

**Fixes**

- Release notes now correctly display on the GitHub Releases page.
- Download zip now ships as a single-file executable inside the `ES-DE Updater` folder (matching v1.0.0 structure).

### What's New in v1.1.2

**Fixes**

- **Package inside Current no longer blocked for the updater's own folder** — a Package stored under `ES-DE Updater\packages\` (where Download Latest puts it) is now accepted; the `ES-DE Updater` top-level folder is preserved by the delete and copy steps, so the package is never at risk. Other nested package locations are still refused.
- **Running-program guard no longer flags the updater itself** — the updater's own process is exempt by path, and its executable stored directly on the ES-DE root (e.g. `D:\ES-DE\ES-DE Updater.exe`) is recognized as a preserved updater entry.
- **Delete sweep can never delete the running updater** — the running updater's own executable is skipped by the delete step even when it sits in the destructive scope.
- **Path comparison hardening** — process paths and the updater's own folder are normalized (long-name expansion) before comparison, closing 8.3 short-name mismatches; the updater's location is canonicalized (reparse points resolved) before the overlap checks in the location gate.
- **Unified preserved-entry rules** — the preserve checks in the location gate, the running-program guard, the delete sweep and the copy list share one source of truth (`FolderNames.IsPreservedTopLevel` / `IsUpdaterEntry`), matching the prefix rule already used by executable and version detection.

### What's New in v1.2.1

**Refactor (no behavior change)**

- The update pipeline moved out of `MainForm` into a new `UpdateOrchestrator` class (plan building, preview message, backup → data-folder rename → delete sweep → copy execution), and the download flow moved into a new `DownloadManager` class (release check, confirmation, download/MD5/extract/validate). `MainForm` now only handles the UI and delegates to these classes.
- All duplicated pipeline logic was removed from `MainForm`, and unit tests were added for `UpdateOrchestrator` and `DownloadManager`.

### What's New in v1.2.0

**Advanced exclusions + portable.txt support**

- **Advanced… window** — a new button opens the Excluded Items dialog for the selected Current folder: every top-level folder and file is listed with a checkbox. Checking an item keeps it during an update — it is never deleted **and** never overwritten by the package copy. Required items (`Emulators`, `ROMs`, `ES-DE`/`.emulationstation`, `Backup`, the updater) are locked and always kept, and the data-folder rows carry a grey note explaining the 2.x ↔ 3.x rename behavior. A **Restore Defaults** button resets the exclusion list to the default state.
- **Remember exclusions** — a toggle in the Advanced window persists the list across sessions (default on). On startup the saved names are validated against the remembered Current folder: entries that no longer exist are dropped and reported in the log, mirroring the saved-folder warning behavior.
- **portable.txt redirects** — when portable.txt contains a path, that location is now authoritative: the data folder is detected, validated, renamed (for 2.x ↔ 3.x crossings) and backed up at the redirected base instead of the root. `portable.txt` itself and the redirected location inside the Current folder are automatically kept (not deleted, and the package's empty portable.txt is not copied over them); redirects pointing outside the folder are kept as well.
- **Fail-closed data-folder rename** — if both `ES-DE` and `.emulationstation` data folders exist where the rename would land, the update stops with a clear message instead of silently skipping and orphaning one of them.
- Clearer validation errors when a Current folder's user data lives at a `portable.txt` path.

### What's New in v1.1.0

**Hard failure-safety layer**

- **Fail-closed path canonicalization** — paths with quotes, wildcards, control characters, UNC paths, or unresolvable junctions are rejected before anything runs.
- **Protected-area gates** — the Current ES-DE folder is refused on drive roots, inside Windows, Program Files, ProgramData, the user profile, and `$Recycle.Bin`. The Package is refused only as a drive root.
- **Physical identity checks** — the Current and Package folders must be two different physical directories (volume serial + file index, resolved through junctions, 8.3 names, and drive aliases); neither may contain the other.
- **Running-program guard** — programs running from the destructive scope block the update; programs in preserved folders (Emulators, ROMs, user data, Backup, the updater) are ignored.
- **Folder seal** — the Current folder's physical identity is re-checked before the rename, the delete, and the copy; if it changed mid-update, the update stops immediately.
- **Updater-overlap rule** — the sweep may never delete the running updater. The updater can be stored inside any preserved folder (e.g. `ES-DE Updater`); layouts where it would be deleted are refused.
- **Strict package executable rule** — the Package must contain `ES-DE*.exe` (modern) or `EmulationStation*.exe` (older 2.x); `ES-DE Updater.exe` is excluded so it cannot masquerade as ES-DE. The Current folder has no executable requirement (repair mode).
- **Version detection skip** — `ES-DE Updater.exe` is now properly skipped when detecting ES-DE versions.

**Improved reliability**

- **Download stall timeout** — if no data is received for 90 seconds, the download fails with a clear message.
- **Smart ZIP unwrap** — the single-root extraction now works even when loose files (readme, checksums) sit alongside the root folder.
- **Robocopy hardened** — `/R:1 /W:1 /XJ` fails fast on locked files and never follows junctions.

**User experience**

- **Repair mode** — a Current folder without an executable is accepted and repaired from the Package; a banner warns the user.
- **Post-update guidance** — the status log now shows next steps (theme updates, system directories, custom_systems).
- **Saved-path warnings** — if a saved Current or Package folder no longer exists, a warning appears at startup.
- Browse dialogs show per-field titles ("Invalid Current ES-DE Folder" / "Invalid Package Folder") and a hint when the user accidentally picks a data folder.

### Links

- [Full documentation](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/blob/main/DOCUMENTATION.md)
- [License (MIT)](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/blob/main/LICENSE)
