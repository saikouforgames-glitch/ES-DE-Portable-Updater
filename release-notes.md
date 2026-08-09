## ES-DE Portable Updater v1.1.0

A standalone Windows utility that refreshes a portable [ES-DE](https://es-de.org/) installation — upgrades, downgrades, and same-version repairs — while preserving your games, emulators, and settings.

> **Windows only** — uses Robocopy and Windows Forms (`net8.0-windows`). Requires 64-bit Windows; does not run on Linux or macOS.

### Screenshots

![Main window (dark theme)](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/raw/main/main%20menu.png)
![Settings window](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/raw/main/settings%20menu.png)

### Install

1. Download `ES-DE-Updater-v1.1.0.zip`.
2. Extract it anywhere.
3. Copy the **`ES-DE Updater`** folder onto the **root** of your ES-DE program folder.
4. Run `ES-DE Updater.exe` inside that folder.

The `ES-DE Updater` folder is preserved across every update — the updater never deletes or copies it.

### Features

- Upgrade / Downgrade / Repair — direction detected automatically
- Data folder rename (`ES-DE` ↔ `.emulationstation`) when moving between versions
- Clean program refresh — old files removed, fresh copies installed
- User data preserved — `Emulators`, `ROMs`, settings never touched
- Folder validation — detects swapped folders, fresh extracts, missing executables
- Optional backup — off by default, backs up selected data folders before the update
- Disk space check — blocks the update if insufficient free space
- Download Latest — fetches the newest stable release from GitLab, verifies MD5, extracts
- Auto-cleanup — removes downloaded package after a successful update
- Themes — System / Light / Dark

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
