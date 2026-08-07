using System.Windows.Forms;

namespace ESDEUpdater;

public class ThemedButton : Button
{
    public Color DisabledBackColorValue { get; set; } = Color.FromArgb(55, 55, 55);
    public Color DisabledForeColorValue { get; set; } = Color.White;
    public Color DisabledBorderColorValue { get; set; } = Color.FromArgb(70, 70, 70);

    protected override void OnPaint(PaintEventArgs pevent)
    {
        if (!Enabled)
        {
            var g = pevent.Graphics;
            using var backBrush = new SolidBrush(DisabledBackColorValue);
            g.FillRectangle(backBrush, ClientRectangle);

            using var borderPen = new Pen(DisabledBorderColorValue);
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, DisabledForeColorValue, DisabledBackColorValue,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        else
        {
            base.OnPaint(pevent);
        }
    }
}
