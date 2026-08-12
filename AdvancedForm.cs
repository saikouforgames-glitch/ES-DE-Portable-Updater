namespace ESDEUpdater;

using System.Windows.Forms.VisualStyles;

public partial class AdvancedForm : Form
{
    private readonly AppSettings _settings;
    private readonly string _currentPath;
    private readonly List<Entry> _entries = [];
    private bool _loading = true;

    private sealed record Entry(string Name, bool IsDirectory, bool Locked, bool Auto, bool Stale);

    public AdvancedForm(AppSettings settings, string currentPath, IEnumerable<string> activeExclusions)
    {
        _settings = settings;
        _currentPath = currentPath;
        InitializeComponent();
        ThemeService.ApplyTheme(this, settings.Theme);
        LoadEntries(currentPath, activeExclusions);
        chkRememberExclusions.Checked = settings.RememberExclusions;
        _loading = false;
        UpdateCountLabel();
    }

    public IReadOnlyCollection<string> ExclusionNames { get; private set; } = [];

    public bool RememberExclusions => chkRememberExclusions.Checked;

    private void LoadEntries(string currentPath, IEnumerable<string> activeExclusions)
    {
        var active = new HashSet<string>(activeExclusions, StringComparer.OrdinalIgnoreCase);
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var auto = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var redirectBase = FolderAnalyzer.TryResolvePortableDataBase(currentPath, out var topLevelSegment);
        if (redirectBase is not null)
        {
            auto.Add(FolderAnalyzer.PortableTxt);
            if (topLevelSegment is not null)
            {
                auto.Add(topLevelSegment);
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(currentPath))
        {
            var name = Path.GetFileName(directory);
            existing.Add(name);
            _entries.Add(new Entry(
                name,
                IsDirectory: true,
                Locked: FolderNames.IsPreservedTopLevel(name) || auto.Contains(name),
                Auto: auto.Contains(name),
                Stale: false));
        }

        foreach (var file in Directory.EnumerateFiles(currentPath))
        {
            var name = Path.GetFileName(file);
            existing.Add(name);
            _entries.Add(new Entry(
                name,
                IsDirectory: false,
                Locked: FolderNames.IsPreservedTopLevel(name) || auto.Contains(name),
                Auto: auto.Contains(name),
                Stale: false));
        }

        foreach (var saved in _settings.ExcludedTopLevelNames)
        {
            if (existing.Contains(saved))
            {
                continue;
            }

            _entries.Add(new Entry(saved, IsDirectory: false, Locked: false, Auto: false, Stale: true));
        }

        foreach (var entry in _entries)
        {
            var display = entry.Name + (entry.IsDirectory ? "  (folder)" : "  (file)");
            if (entry.Stale)
            {
                display += "  — no longer exists";
            }

            lstItems.Items.Add(display);
            var checkedState = entry.Locked || (!entry.Stale && active.Contains(entry.Name));
            lstItems.SetItemChecked(lstItems.Items.Count - 1, checkedState);
        }
    }

    private void LstItems_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var entry = _entries[e.Index];
        if (entry.Locked)
        {
            e.NewValue = CheckState.Checked;
        }
        else if (entry.Stale)
        {
            e.NewValue = CheckState.Unchecked;
        }

        UpdateCountLabel(e.Index, e.NewValue == CheckState.Checked);
    }

    private void UpdateCountLabel(int? changedIndex = null, bool? changedChecked = null)
    {
        var checkedCount = 0;
        var lockedCount = 0;
        for (var i = 0; i < _entries.Count; i++)
        {
            var isChecked = _entries[i].Locked || lstItems.GetItemChecked(i);
            if (i == changedIndex && changedChecked is not null)
            {
                isChecked = changedChecked.Value;
            }

            if (isChecked)
            {
                checkedCount++;
            }

            if (_entries[i].Locked)
            {
                lockedCount++;
            }
        }

        lblCount.Text = $"{_entries.Count} item(s) in the Current folder — {checkedCount} checked (kept), {lockedCount} required.";
    }

    private void LstItems_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _entries.Count)
        {
            return;
        }

        var entry = _entries[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) != 0;

        using var backBrush = new SolidBrush(isSelected ? SystemColors.Highlight : lstItems.BackColor);
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        var checkState = lstItems.GetItemCheckState(e.Index) switch
        {
            CheckState.Checked => CheckBoxState.CheckedNormal,
            CheckState.Indeterminate => CheckBoxState.MixedNormal,
            _ => CheckBoxState.UncheckedNormal
        };
        CheckBoxRenderer.DrawCheckBox(
            e.Graphics,
            new Point(e.Bounds.Left + 6, e.Bounds.Top + (e.Bounds.Height - 15) / 2),
            checkState);

        var font = e.Font ?? lstItems.Font;
        var textTop = e.Bounds.Top + (e.Bounds.Height - font.Height) / 2 + 1;
        var textStart = e.Bounds.Left + 26;
        var itemFore = isSelected ? SystemColors.HighlightText : lstItems.ForeColor;
        var grayFore = isSelected ? SystemColors.HighlightText : SystemColors.GrayText;

        if (entry.Stale)
        {
            TextRenderer.DrawText(
                e.Graphics,
                entry.Name + "  (file) — no longer exists",
                font,
                new Point(textStart, textTop),
                grayFore);
            return;
        }

        if (entry.Locked)
        {
            textStart += 4;
        }

        var text = entry.Name + (entry.IsDirectory ? "  (folder)" : "  (file)");
        var nameWidth = TextRenderer.MeasureText(e.Graphics, text, font).Width;
        TextRenderer.DrawText(e.Graphics, text, font, new Point(textStart, textTop), itemFore);

        var renameNote = GetDataFolderRenameNote(entry.Name);
        if (renameNote is not null)
        {
            var noteStart = textStart + nameWidth + 4;
            var noteWidth = TextRenderer.MeasureText(e.Graphics, renameNote, font).Width;
            var maxNoteWidth = Math.Max(0, e.Bounds.Right - noteStart - 2);

            if (noteWidth > maxNoteWidth)
            {
                renameNote = TruncateToWidth(e.Graphics, renameNote, font, maxNoteWidth);
            }

            TextRenderer.DrawText(
                e.Graphics,
                renameNote,
                font,
                new Point(noteStart, textTop),
                grayFore,
                TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping);
        }
    }

    private static string TruncateToWidth(Graphics g, string text, Font font, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        const string ellipsis = "\u2026";
        var ellipsisWidth = TextRenderer.MeasureText(g, ellipsis, font).Width;
        var available = maxWidth - ellipsisWidth;
        if (available <= 0)
        {
            return ellipsis;
        }

        for (var length = text.Length; length > 0; length--)
        {
            var candidate = text[..length];
            if (TextRenderer.MeasureText(g, candidate, font).Width <= available)
            {
                return candidate + ellipsis;
            }
        }

        return ellipsis;
    }

    private static string? GetDataFolderRenameNote(string name)
    {
        if (string.Equals(name, FolderNames.EmulationStation, StringComparison.OrdinalIgnoreCase))
        {
            return "renamed to ES-DE when upgrading to 3.x";
        }

        if (string.Equals(name, FolderNames.EsDe, StringComparison.OrdinalIgnoreCase))
        {
            return "renamed to .emulationstation when downgrading to 2.x";
        }

        return null;
    }

    private void BtnRestoreDefaults_Click(object? sender, EventArgs e)
    {
        chkRememberExclusions.Checked = true;

        for (var i = 0; i < _entries.Count; i++)
        {
            if (!_entries[i].Locked)
            {
                lstItems.SetItemChecked(i, false);
            }
        }

        UpdateCountLabel();
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _entries.Count; i++)
        {
            if (lstItems.GetItemChecked(i) && !_entries[i].Stale)
            {
                chosen.Add(_entries[i].Name);
            }
        }

        ExclusionNames = chosen;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}