using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ESDEUpdater;

public class ThemedCheckBox : CheckBox
{
    private const int TextGlyphGap = 4;

    public Color DisabledForeColorValue { get; set; } = SystemColors.GrayText;

    protected override void OnPaint(PaintEventArgs pevent)
    {
        if (!VisualStyleRenderer.IsSupported)
        {
            base.OnPaint(pevent);
            return;
        }

        base.OnPaintBackground(pevent);

        var g = pevent.Graphics;
        var state = CheckState switch
        {
            CheckState.Checked => Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled,
            CheckState.Indeterminate => Enabled ? CheckBoxState.MixedNormal : CheckBoxState.MixedDisabled,
            _ => Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled
        };

        var glyphSize = CheckBoxRenderer.GetGlyphSize(g, state);
        var glyphLocation = new Point(0, (Height - glyphSize.Height) / 2);
        CheckBoxRenderer.DrawCheckBox(g, glyphLocation, state);

        var foreColor = Enabled ? ForeColor : DisabledForeColorValue;
        var textBounds = new Rectangle(glyphSize.Width + TextGlyphGap, 0, Width - glyphSize.Width - TextGlyphGap, Height);
        TextRenderer.DrawText(g, Text, Font, textBounds, foreColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
