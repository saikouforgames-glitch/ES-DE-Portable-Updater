namespace ESDEUpdater;

public static class EsDeValidation
{
    private static readonly string[] RequiredFolders = [FolderNames.Emulators, FolderNames.Roms];

    private const int MinRomCountForLikelyPopulated = 10;
    private const int MinEmulatorCountForLikelyPopulated = 2;
    private const int MaxCountForLikelyFresh = 1;

    /// <summary>
    /// Quick browse-time check for the Old ES-DE folder.
    /// Returns null when the folder is acceptable, or a user-facing error string.
    /// </summary>
    public static string? ValidateOldFolder(string? path)
    {
        var gateError = ValidationGate.CheckOldLocation(path);
        if (gateError is not null)
        {
            return gateError;
        }

        return ValidateSingleFolder(path, isOld: true);
    }

    /// <summary>
    /// Quick browse-time check for the New ES-DE folder.
    /// Returns null when the folder is acceptable, or a user-facing error string.
    /// </summary>
    public static string? ValidateNewFolder(string? path)
    {
        var gateError = ValidationGate.CheckNewLocation(path);
        if (gateError is not null)
        {
            return gateError;
        }

        return ValidateSingleFolder(path, isOld: false);
    }

    private static string? ValidateSingleFolder(string? path, bool isOld)
    {
        var label = isOld ? "Current" : "Package";

        if (string.IsNullOrWhiteSpace(path))
        {
            return $"The {label} ES-DE folder path is empty.";
        }

        var fullPath = path.Trim();
        if (!Directory.Exists(fullPath))
        {
            return $"The {label} ES-DE folder does not exist:\n{fullPath}";
        }

        if (!isOld && !FolderAnalyzer.HasEsDeExecutable(fullPath))
        {
            return
                "No ES-DE executable was found in the Package folder:\n" +
                fullPath + "\n\n" +
                "A freshly extracted ES-DE portable package always contains " +
                "ES-DE.exe (modern releases) or EmulationStation.exe (older 2.x releases). " +
                "If the executable was renamed or removed, this folder cannot be installed as a package.";
        }

        if (isOld)
        {
            var dataFolderError = CheckEsDeStructurePresence(fullPath);
            if (dataFolderError is not null)
            {
                return dataFolderError;
            }
        }

        return null;
    }

    /// <summary>
    /// Cheap browse-time structure check for the Current folder: the three ES-DE
    /// signature folders must exist (Emulators, ROMs, and a user data folder).
    /// No counting and no executable requirement, so an installation whose
    /// executable is missing or damaged still passes (repair mode).
    /// </summary>
    private static string? CheckEsDeStructurePresence(string rootPath)
    {
        foreach (var folderName in RequiredFolders)
        {
            if (!Directory.Exists(Path.Combine(rootPath, folderName)))
            {
                var message =
                    $"The Current ES-DE folder is missing the \"{folderName}\" directory.\n\n" +
                    "A normal ES-DE portable installation contains Emulators, ES-DE, and ROMs folders.\n\n" +
                    "This may not be a valid ES-DE installation, or you may have selected the wrong folder.";

                if (LooksLikeDataFolder(rootPath))
                {
                    message +=
                        "\n\nYou selected a folder named \"" + Path.GetFileName(rootPath) +
                        "\", which is the ES-DE user data folder inside a package.\n" +
                        "Select the folder that contains it (the package root, where ES-DE.exe sits) as the Package instead.";
                }

                return message;
            }
        }

        if (FolderAnalyzer.FindEsDeDataFolderInfo(rootPath) is null)
        {
            var message =
                "No ES-DE user data folder was found in the Current ES-DE folder.\n\n" +
                "An ES-DE installation stores its user data (settings, gamelists, themes) in a folder named " +
                "\"ES-DE\" (version 3.0.0 and newer) or \".emulationstation\" (older releases).\n\n" +
                "What to do:\n" +
                "Verify you selected the folder that contains your existing ES-DE installation.";

            var redirectBase = FolderAnalyzer.TryResolvePortableDataBase(rootPath, out _);
            if (redirectBase is not null)
            {
                message +=
                    "\n\n" + BuildPortableTxtRedirectMessage(redirectBase);
            }

            return message;
        }

        return null;
    }

    private static bool LooksLikeDataFolder(string rootPath)
    {
        var name = Path.GetFileName(rootPath.TrimEnd('\\', '/'));
        return string.Equals(name, FolderNames.EsDe, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, FolderNames.EmulationStation, StringComparison.OrdinalIgnoreCase);
    }

    public static ValidationResult ValidateForUpdate(string oldPath, string newPath)
    {
        var oldPathIsEmpty = string.IsNullOrWhiteSpace(oldPath);
        var newPathIsEmpty = string.IsNullOrWhiteSpace(newPath);

        if (oldPathIsEmpty && newPathIsEmpty)
        {
            return ValidationResult.Failure(
                "Validation Failed",
                "The Current ES-DE folder path is empty.\n\nPlease click Browse next to \"Current ES-DE\" and select your current installation.");
        }

        if (!oldPathIsEmpty && !Directory.Exists(oldPath.Trim()))
        {
            return ValidationResult.Failure(
                "Validation Failed",
                $"The Current ES-DE folder does not exist:\n{oldPath.Trim()}\n\nPlease verify the path and try again.");
        }

        if (!newPathIsEmpty && !Directory.Exists(newPath.Trim()))
        {
            return ValidationResult.Failure(
                "Validation Failed",
                $"The Package folder does not exist:\n{newPath.Trim()}\n\nPlease verify the path and try again.");
        }

        if (!oldPathIsEmpty)
        {
            var oldGateError = ValidationGate.CheckOldLocation(oldPath);
            if (oldGateError is not null)
            {
                return ValidationResult.Failure("Validation Failed", oldGateError);
            }
        }

        if (!newPathIsEmpty)
        {
            var newGateError = ValidationGate.CheckNewLocation(newPath);
            if (newGateError is not null)
            {
                return ValidationResult.Failure("Validation Failed", newGateError);
            }
        }

        if (!oldPathIsEmpty && !newPathIsEmpty)
        {
            var relationshipError = ValidationGate.CheckRelationship(oldPath, newPath);
            if (relationshipError is not null)
            {
                return ValidationResult.Failure("Validation Failed", relationshipError);
            }
        }

        FolderAnalysis? old = null;
        FolderAnalysis? newAnalysis = null;

        if (!oldPathIsEmpty)
        {
            old = FolderAnalyzer.Analyze(
                oldPath.Trim(),
                SupportedRomExtensions.GetSupportedExtensions(oldPath.Trim()));
        }

        if (!newPathIsEmpty)
        {
            newAnalysis = FolderAnalyzer.Analyze(
                newPath.Trim(),
                SupportedRomExtensions.GetSupportedExtensions(newPath.Trim()));
        }

        if (oldPathIsEmpty)
        {
            var newError = ValidateSingleNewFolder(newAnalysis!);
            if (newError is not null)
            {
                return newError;
            }

            return ValidationResult.Failure(
                "Validation Failed",
                "The Current ES-DE folder path is empty.\n\n" +
                "Your Package folder was verified and looks like a freshly extracted version.\n\n" +
                "Please click Browse next to \"Current ES-DE\" and select your current installation. " +
                "It must be a different folder that contains the ES-DE program.");
        }

        if (newPathIsEmpty)
        {
            var oldError = ValidateSingleOldFolder(old!);
            if (oldError is not null)
            {
                return oldError;
            }

            return ValidationResult.Failure(
                "Validation Failed",
                "The Package folder path is empty.\n\n" +
                "Your Current ES-DE folder was verified and looks like a valid installation.\n\n" +
                "Please click Browse next to \"Package\" and select the ES-DE version you extracted. " +
                "It must be a different folder that contains the ES-DE program.");
        }

        var oldBasics = CheckFolderBasics(old!, isOld: true, other: newAnalysis);
        if (oldBasics is not null)
        {
            return oldBasics;
        }

        var newBasics = CheckFolderBasics(newAnalysis!, isOld: false, other: old);
        if (newBasics is not null)
        {
            return newBasics;
        }

        var reversal = DetectFolderReversal(old!, newAnalysis!);
        if (reversal is not null)
        {
            return reversal;
        }

        var oldProfileError = CheckOldProfile(old!, newAnalysis);
        if (oldProfileError is not null)
        {
            return oldProfileError;
        }

        var newProfileError = CheckNewProfile(newAnalysis!, old);
        if (newProfileError is not null)
        {
            return newProfileError;
        }

        return ValidationResult.Success(old!, newAnalysis!);
    }

    private static ValidationResult? ValidateSingleOldFolder(FolderAnalysis old)
    {
        var gateError = ValidationGate.CheckOldLocation(old.RootPath);
        if (gateError is not null)
        {
            return ValidationResult.Failure(
                "Validation Failed",
                gateError + "\n\n" +
                "Note: the Package folder is also empty. Once the Current ES-DE folder issue is fixed, " +
                "please select the ES-DE version you extracted as the Package folder.",
                old,
                null);
        }

        var error = CheckFolderBasics(old, isOld: true, other: null)
            ?? CheckOldProfile(old, other: null);

        if (error is null)
        {
            return null;
        }

        return ValidationResult.Failure(
            error.Title,
            error.Message + "\n\n" +
            "Note: the Package folder is also empty. Once the Current ES-DE folder issue is fixed, " +
            "please select the ES-DE version you extracted as the Package folder.",
            error.OldAnalysis,
            error.NewAnalysis);
    }

    private static ValidationResult? ValidateSingleNewFolder(FolderAnalysis newAnalysis)
    {
        var gateError = ValidationGate.CheckNewLocation(newAnalysis.RootPath);
        if (gateError is not null)
        {
            return ValidationResult.Failure(
                "Validation Failed",
                gateError + "\n\n" +
                "Note: the Current ES-DE folder is also empty. Once the Package folder issue is fixed, " +
                "please select your current installation as the Current folder.",
                null,
                newAnalysis);
        }

        var error = CheckFolderBasics(newAnalysis, isOld: false, other: null)
            ?? CheckNewProfile(newAnalysis, old: null);

        if (error is null)
        {
            return null;
        }

        return ValidationResult.Failure(
            error.Title,
            error.Message + "\n\n" +
            "Note: the Current ES-DE folder is also empty. Once the Package folder issue is fixed, " +
            "please select your current installation as the Current folder.",
            error.OldAnalysis,
            error.NewAnalysis);
    }

    private static ValidationResult? CheckFolderBasics(FolderAnalysis folder, bool isOld, FolderAnalysis? other)
    {
        var label = isOld ? "Current" : "Package";

        if (!folder.FolderExists)
        {
            return ValidationResult.Failure(
                "Validation Failed",
                $"The {label} ES-DE folder does not exist:\n{folder.RootPath}\n\nPlease verify the path and try again.",
                folder, other);
        }

        if (!isOld && !FolderAnalyzer.HasEsDeExecutable(folder.RootPath))
        {
            return ValidationResult.Failure(
                "Validation Failed",
                "No ES-DE executable was found in the Package folder.\n\n" +
                "A freshly extracted ES-DE portable package always contains " +
                "ES-DE.exe (modern releases) or EmulationStation.exe (older 2.x releases).\n\n" +
                "What to do:\n" +
                "Extract the new ES-DE release and select the root folder that contains the program.",
                folder, other);
        }

        foreach (var folderName in RequiredFolders)
        {
            if (!Directory.Exists(Path.Combine(folder.RootPath, folderName)))
            {
                var message = isOld
                    ? $"The Current ES-DE folder is missing the \"{folderName}\" directory.\n\n" +
                      "A normal ES-DE portable installation contains Emulators, ES-DE, and ROMs folders.\n\n" +
                      "This may not be a valid ES-DE installation, or you may have selected the wrong folder."
                    : $"The Package folder is missing the \"{folderName}\" directory.\n\n" +
                      "The freshly extracted ES-DE package should contain Emulators, ES-DE, and ROMs folders.\n\n" +
                      "Try extracting the ES-DE archive again.";

                return ValidationResult.Failure("Validation Failed", message, folder, other);
            }
        }

        if (!folder.HasEsDeDataFolder)
        {
            var message = isOld
                ? "No ES-DE user data folder was found in the Current ES-DE folder.\n\n" +
                  "An ES-DE installation stores its user data (settings, gamelists, themes) in a folder named " +
                  "\"ES-DE\" (version 3.0.0 and newer, from 17 February 2024) or \".emulationstation\" (older releases, before 3.0.0).\n\n" +
                  "What to do:\n" +
                  "Verify you selected the folder that contains your existing ES-DE installation."
                : "No ES-DE user data folder was found in the Package folder.\n\n" +
                  "A freshly extracted ES-DE package contains its user data folder (\"ES-DE\" for 3.0.0 and newer, " +
                  "\".emulationstation\" for older releases), even if it is empty.\n\n" +
                  "What to do:\n" +
                  "Try extracting the ES-DE archive again.";

            if (isOld)
            {
                var redirectBase = FolderAnalyzer.TryResolvePortableDataBase(folder.RootPath, out _);
                if (redirectBase is not null)
                {
                    message +=
                        "\n\n" + BuildPortableTxtRedirectMessage(redirectBase);
                }
            }

            return ValidationResult.Failure("Validation Failed", message, folder, other);
        }

        return null;
    }

    private static ValidationResult? DetectFolderReversal(FolderAnalysis old, FolderAnalysis newAnalysis)
    {
        var comparison = BuildComparisonSummary(old, newAnalysis);

        var oldLooksFresh = old.RomFileCount == 0 && old.EmulatorFolderCount == 0;
        var newLooksPopulated = newAnalysis.RomFileCount > 0 && newAnalysis.EmulatorFolderCount > 0;

        if (oldLooksFresh && newLooksPopulated)
        {
            return ValidationResult.Failure(
                "Folders Appear Reversed",
                "The selected folders appear to be reversed.\n\n" +
                comparison + "\n\n" +
                "The folder selected as \"Current ES-DE\" does not appear to contain your existing games or emulator installation.\n\n" +
                "The folder selected as \"Package\" appears to contain your existing ROM collection and emulators.\n\n" +
                "Continuing could overwrite or replace your current installation.\n\n" +
                "What to do:\n" +
                "Swap the folder selections and try again.",
                old, newAnalysis);
        }

        var newLooksFresh = newAnalysis.RomFileCount == 0 && newAnalysis.EmulatorFolderCount == 0;
        if (oldLooksFresh && newLooksFresh)
        {
            return ValidationResult.Failure(
                "Validation Failed",
                "Both selected folders appear to be newly extracted ES-DE packages.\n\n" +
                comparison + "\n\n" +
                "Neither folder contains emulator folders or game files.\n\n" +
                "Your \"Current ES-DE\" folder must be your existing installation, which normally contains " +
                "emulator folders (such as RetroArch-Win64) and game files inside its system folders.\n\n" +
                "What to do:\n" +
                "Select the folder where your current ES-DE data lives as Old, and a freshly extracted package as New.",
                old, newAnalysis);
        }

        var newClearlyMorePopulated =
            newAnalysis.RomFileCount > old.RomFileCount &&
            newAnalysis.EmulatorFolderCount > old.EmulatorFolderCount &&
            newAnalysis.RomFileCount >= MinRomCountForLikelyPopulated &&
            newAnalysis.EmulatorFolderCount >= MinEmulatorCountForLikelyPopulated &&
            old.RomFileCount <= MaxCountForLikelyFresh &&
            old.EmulatorFolderCount <= MaxCountForLikelyFresh;

        if (newClearlyMorePopulated)
        {
            return ValidationResult.Failure(
                "Folders May Be Reversed",
                "The folder comparison suggests you may have swapped Current and Package.\n\n" +
                comparison + "\n\n" +
                "Your \"Package\" folder contains significantly more ROM files and emulator folders than your \"Current ES-DE\" folder.\n\n" +
                "Normally, the Current folder is your existing installation and the Package folder is a freshly extracted version.\n\n" +
                "What to do:\n" +
                "Double-check both folder selections before continuing.",
                old, newAnalysis);
        }

        return null;
    }

    private static ValidationResult? CheckOldProfile(FolderAnalysis old, FolderAnalysis? other)
    {
        var summary = other is null ? string.Empty : BuildComparisonSummary(old, other) + "\n\n";

        if (old.EmulatorsIsEmpty)
        {
            var explanation = other is null
                ? "This folder does not appear to be your existing ES-DE installation, which should contain your installed emulators."
                : "This may indicate that you selected the newly extracted ES-DE folder instead of your current installation.";

            return ValidationResult.Failure(
                "Validation Failed",
                "The selected Current ES-DE folder contains an empty Emulators directory.\n\n" +
                summary +
                "Normally, an existing ES-DE installation contains emulator folders such as RetroArch-Win64, DuckStation, or PCSX2.\n\n" +
                explanation + "\n\n" +
                "What to do:\n" +
                "Select the folder where your emulators are already installed.",
                old, other);
        }

        if (!old.HasRomFiles)
        {
            var explanation = other is null
                ? "This may indicate that you selected the wrong folder."
                : "This may indicate that you selected the wrong folder, or the Package folder was selected as Current.";

            return ValidationResult.Failure(
                "Validation Failed",
                "No game files were found in the Current ES-DE ROMs folder.\n\n" +
                summary +
                "The updater searched the ROMs folder and all system subfolders for supported ROM file extensions " +
                "(using the es_systems.xml definitions of this installation) and found none.\n\n" +
                "An existing ES-DE installation normally contains game files inside system folders like nes, snes, ps2, or gba.\n\n" +
                explanation + "\n\n" +
                "What to do:\n" +
                "Verify you selected the folder that contains your ROM collection.",
                old, other);
        }

        return null;
    }

    private static ValidationResult? CheckNewProfile(FolderAnalysis newAnalysis, FolderAnalysis? old)
    {
        var summary = old is null ? string.Empty : BuildComparisonSummary(old, newAnalysis) + "\n\n";

        if (!newAnalysis.EmulatorsIsEmpty)
        {
            var explanation = old is null
                ? "This folder does not appear to be a freshly extracted ES-DE package."
                : "This may indicate that you selected your current installation as the Package folder.";

            return ValidationResult.Failure(
                "Validation Failed",
                $"The selected Package folder already contains {newAnalysis.EmulatorFolderCount} emulator folder(s).\n\n" +
                summary +
                "A freshly extracted ES-DE package normally has an empty Emulators directory.\n\n" +
                explanation + "\n\n" +
                "What to do:\n" +
                "Select the folder from the newly extracted ES-DE release, not your existing installation.",
                old, newAnalysis);
        }

        if (newAnalysis.HasRomFiles)
        {
            var explanation = old is null
                ? "This folder does not appear to be a freshly extracted ES-DE package."
                : "This strongly suggests you selected your current installation as the Package folder.";

            return ValidationResult.Failure(
                "Validation Failed",
                $"The selected Package folder already contains {newAnalysis.RomFileCount:N0} ROM file(s).\n\n" +
                summary +
                "A freshly extracted ES-DE package should not contain game files in the ROMs folder.\n\n" +
                explanation + "\n\n" +
                "What to do:\n" +
                "Extract the latest ES-DE portable package to a new location and select that folder.",
                old, newAnalysis);
        }

        return null;
    }

    public static string BuildComparisonSummary(FolderAnalysis old, FolderAnalysis newAnalysis)
    {
        return
            "Folder comparison:\n\n" +
            $"Current (in use):\n" +
            $"  ROM files: {old.RomFileCount:N0}\n" +
            $"  Emulator folders: {old.EmulatorFolderCount:N0}\n\n" +
            $"Package (to apply):\n" +
            $"  ROM files: {newAnalysis.RomFileCount:N0}\n" +
            $"  Emulator folders: {newAnalysis.EmulatorFolderCount:N0}";
    }

    private static string BuildPortableTxtRedirectMessage(string redirectBase) =>
        "portable.txt in this folder points the user data to:\n" + redirectBase +
        "\nNo \"ES-DE\" or \".emulationstation\" folder was found there either.";
}
