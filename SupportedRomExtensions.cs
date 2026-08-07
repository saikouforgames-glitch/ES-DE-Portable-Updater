using System.Xml.Linq;

namespace ESDEUpdater;

/// <summary>
/// Supported ROM file extensions recognized by ES-DE.
/// Primary source: the <c>resources\systems\windows\es_systems.xml</c> file shipped with
/// each ES-DE installation. Fallback: the built-in list below, so validation still works
/// even if the es_systems.xml file is missing or damaged.
/// </summary>
public static class SupportedRomExtensions
{
    /// <summary>Platform-specific es_systems.xml path, relative to an ES-DE portable root.</summary>
    private static readonly string[] SystemFileCandidates =
    [
        Path.Combine("resources", "systems", "windows", "es_systems.xml"),
        Path.Combine("resources", "systems", "es_systems.xml")
    ];

    private static readonly HashSet<string> BuiltInExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".7z", ".zip", ".rar",
        ".chd", ".iso", ".bin", ".cue", ".img", ".mdf", ".mds", ".ccd", ".sub", ".nrg",
        ".cdi", ".gdi", ".cso", ".pbp", ".rvz", ".gcz", ".wbfs", ".wia", ".isz",
        ".dol", ".elf", ".dff", ".tgc", ".wad", ".gcm", ".ciso",
        ".nes", ".unf", ".unif", ".fds", ".nsf", ".nsfe",
        ".sfc", ".smc", ".swc", ".fig", ".bs", ".st",
        ".gb", ".gbc", ".gba", ".sgb",
        ".nds", ".dsi", ".3ds", ".cxi", ".3dsx", ".cia",
        ".n64", ".z64", ".v64", ".ndd",
        ".gen", ".md", ".smd", ".32x", ".sms", ".gg", ".sg",
        ".pce", ".sgx",
        ".ngp", ".ngc", ".npc",
        ".ws", ".wsc",
        ".a26", ".a52", ".a78",
        ".col", ".cv",
        ".int", ".vec",
        ".prg", ".d64", ".d71", ".d81", ".d80", ".g64", ".g41", ".x64", ".t64", ".tap", ".crt",
        ".adf", ".adz", ".ipf", ".dms", ".hdf", ".lha",
        ".atr", ".atx", ".xex", ".cas", ".rom", ".bin8",
        ".dsk", ".mgt", ".sad", ".sbt", ".d88", ".1dd",
        ".k7", ".sap",
        ".m3u",
        ".xiso", ".xbe", ".xbox",
        ".apk", ".app",
        ".exe", ".bat", ".com", ".conf", ".dosz",
        ".scummvm", ".ps3",
        ".eboot",
        ".vpk",
        ".wasm",
        ".mx1", ".mx2",
        ".min", ".mkf", ".msu1",
        ".j64", ".jmm",
        ".z80", ".sna", ".szx",
        ".hdm", ".x1", ".t77",
        ".lnx",
        ".vb", ".vboy",
        ".iwad", ".pwad",
        ".love", ".pk3", ".pk4",
        ".swf",
        ".trd", ".scl", ".fdi",
        ".gam",
        ".x", ".pof", ".pok",
        ".mcr", ".m1",
        ".tvc", ".dint", ".dum",
        ".gbz",
        ".dx2",
        ".prc", ".pqs",
        ".mega", ".stu",
        ".gsp",
        ".fd",
        ".cdt", ".voc",
        ".8080",
    };

    /// <summary>
    /// Returns the set of supported ROM extensions for the given ES-DE portable installation.
    /// Reads <c>es_systems.xml</c> first; falls back to the built-in list if it cannot be read.
    /// </summary>
    public static IReadOnlyCollection<string> GetSupportedExtensions(string esDeRootPath)
    {
        var fromXml = TryLoadFromEsSystemsXml(esDeRootPath);
        return fromXml ?? BuiltInExtensions;
    }

    public static bool IsSupportedRomFile(string filePath, IReadOnlyCollection<string>? extensions = null)
    {
        var set = extensions ?? BuiltInExtensions;
        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && set.Contains(extension);
    }

    private static HashSet<string>? TryLoadFromEsSystemsXml(string esDeRootPath)
    {
        if (string.IsNullOrWhiteSpace(esDeRootPath) || !Directory.Exists(esDeRootPath))
        {
            return null;
        }

        foreach (var candidate in SystemFileCandidates)
        {
            var fullPath = Path.Combine(esDeRootPath, candidate);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            try
            {
                var doc = XDocument.Load(fullPath);
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var extensionElement in doc.Descendants("extension"))
                {
                    if (string.IsNullOrWhiteSpace(extensionElement.Value))
                    {
                        continue;
                    }

                    foreach (var raw in extensionElement.Value.Split(
                                 new[] { ' ', '\t', '\r', '\n' },
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        var normalized = raw.Trim().ToLowerInvariant();
                        if (string.IsNullOrEmpty(normalized))
                        {
                            continue;
                        }

                        extensions.Add(normalized.StartsWith('.') ? normalized : "." + normalized);
                    }
                }

                return extensions.Count > 0 ? extensions : null;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
