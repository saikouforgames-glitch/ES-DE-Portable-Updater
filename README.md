# ES-DE Portable Updater

![Platform](https://img.shields.io/badge/platform-Windows-blue)

A standalone Windows utility that refreshes a portable [ES-DE (EmulationStation Desktop Edition)](https://es-de.org/) installation — **upgrades**, **downgrades**, and **same-version repairs** — while preserving your games, emulators, and settings.

> **Windows only** — uses Robocopy and Windows Forms (`net8.0-windows`). Requires 64-bit Windows; does not run on Linux or macOS.

## Screenshots

![Main window (dark theme)](main%20menu.png)
![Settings window](settings%20menu.png)

## Features

| Feature | Description |
|---------|-------------|
| **Upgrade / Downgrade / Repair** | Direction detected automatically from the ES-DE executable version |
| **Data folder rename** | Handles the `ES-DE` ↔ `.emulationstation` transition between versions |
| **Clean program refresh** | Old program files are removed first; fresh copies are brought in from the package |
| **User data preserved** | `Emulators`, `ROMs`, and the data folder (`ES-DE` / `.emulationstation`) are never touched |
| **Folder validation** | Detects swapped folders, fresh extracts, and missing executables before anything runs |
| **Optional backup** | Off by default; backs up selected data folders before the update |
| **Disk space check** | Blocks the update if there is not enough free space |
| **Download Latest** | Fetches the latest stable release from GitLab, verifies the MD5, extracts, and sets it as the package |
| **Auto-cleanup** | Removes the downloaded package after a successful upgrade |
| **Themes** | System / Light / Dark |
| **Portable** | No installer, no registry, settings stored beside the exe |

## Quick Start

1. Run `ES-DE Updater.exe`.
2. **Browse** → **Current ES-DE** — select your existing installation.
3. Click **Download Latest** to fetch the newest release, or **Browse** → **Upgrade/Downgrade Package** to select an extracted package manually.
4. Click **Start Upgrade / Start Downgrade / Start Repair** and confirm.

For full details see [DOCUMENTATION.md](DOCUMENTATION.md).

## Install (Release ZIP)

The release ZIP ships as `ES-DE-Updater-v1.0.0.zip` containing a single folder:

```
ES-DE Updater/
└── ES-DE Updater.exe
```

**To install:**

1. Download the release ZIP.
2. Extract it anywhere.
3. Copy (or move) the **`ES-DE Updater`** folder onto the **root** of your ES-DE program folder.
4. Run `ES-DE Updater.exe` inside that folder.

The updater lives inside the `ES-DE Updater` folder — this folder is preserved on every upgrade/downgrade/repair so the updater stays in place.

## Building from Source

Prerequisite: .NET 8 SDK, Windows.

```powershell
# Build
dotnet build ESDEUpdater.csproj -c Release

# Publish self-contained single-file exe
dotnet publish ESDEUpdater.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Project Structure

```
ESDEUpdater/
├── Program.cs
├── MainForm.cs / MainForm.Designer.cs
├── SettingsForm.cs / SettingsForm.Designer.cs
├── AppSettings.cs / SettingsService.cs
├── BackupService.cs / RobocopyService.cs
├── ReleaseService.cs / EsDeVersionService.cs
├── FolderAnalyzer.cs / FolderAnalysis.cs / EsDeValidation.cs
├── FolderNames.cs / PathSafety.cs / ValidationGate.cs
├── ProcessGuard.cs / Diagnostics.cs
├── SupportedRomExtensions.cs / DiskSpaceHelper.cs
├── ThemeService.cs
├── Themed*.cs (Button, CheckBox, ProgressBar, TextBox)
├── DOCUMENTATION.md
└── LICENSE
```

## License

[MIT](LICENSE) — © 2026 Evander Aston
