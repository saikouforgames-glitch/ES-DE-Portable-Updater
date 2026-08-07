using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ESDEUpdater;

public class ThemedTextBox : TextBox
{
    private const int WmNcPaint = 0x0085;

    private Color _borderColorValue = SystemColors.WindowFrame;

    public Color BorderColorValue
    {
        get => _borderColorValue;
        set
        {
            _borderColorValue = value;
            RedrawBorder();
        }
    }

    public ThemedTextBox()
    {
        BorderStyle = BorderStyle.FixedSingle;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        SelectionStart = 0;
        SelectionLength = 0;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WmNcPaint)
        {
            RedrawBorder();
        }
    }

    private void RedrawBorder()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        var hdc = GetWindowDC(Handle);
        if (hdc == IntPtr.Zero)
        {
            return;
        }

        try
        {
            using var g = Graphics.FromHdc(hdc);
            using var pen = new Pen(_borderColorValue);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
        finally
        {
            ReleaseDC(Handle, hdc);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
}
