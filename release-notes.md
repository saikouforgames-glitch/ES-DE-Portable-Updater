# ES-DE Portable Updater v1.2.0

A standalone Windows utility that refreshes a portable [ES-DE](https://es-de.org/) installation — upgrades, downgrades, and same-version repairs — while preserving your games, emulators, and settings.

> **Windows only** — uses Robocopy and Windows Forms (`net8.0-windows`). Requires 64-bit Windows; does not run on Linux or macOS.

### What's New in v1.2.0

**Advanced exclusions + portable.txt support**

* **Advanced exclusions** — choose which files and folders should be preserved during an update.
* **Remember exclusions** — optionally save your exclusion list for future updates.
* **portable.txt support** — correctly handles ES-DE installations that use a redirected data folder.
* **Safer data-folder handling** — prevents conflicting `ES-DE` and `.emulationstation` folders from being silently left behind.
* **Clearer validation messages** — improved errors for portable.txt-based data folders.

**Fixes & improvements**

* Fixed package handling for the updater's own `ES-DE Updater\packages\` folder.
* Fixed the updater being incorrectly detected as a running ES-DE program.
* Prevented the running updater from being deleted during an update.
* Improved path and folder safety checks.
* Unified preserved-folder handling across the update process.

**Refactoring & testing**

* Improved the internal update and download pipeline.
* Added new unit tests for update and download behavior.

### What's New in v1.1.1

* Fixed release notes display on GitHub Releases.
* Updated the download ZIP structure to match the expected `ES-DE Updater` folder layout.

### What's New in v1.1.0

**Hard failure-safety layer**

* Added stronger path and folder safety checks.
* Added protection against updating dangerous or invalid locations.
* Added physical folder identity checks and folder-change detection.
* Added running-program protection.
* Added safer handling of the updater and preserved folders.
* Added stricter ES-DE package validation.

**Improved reliability**

* Added download stall detection.
* Improved ZIP extraction handling.
* Hardened Robocopy behavior.

**User experience**

* Added repair mode for installations missing the ES-DE executable.
* Added post-update guidance.
* Added warnings for missing saved folders.
* Improved folder selection and validation messages.

### Links

* [Full documentation](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/blob/main/DOCUMENTATION.md)
* [License (MIT)](https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/blob/main/LICENSE)
