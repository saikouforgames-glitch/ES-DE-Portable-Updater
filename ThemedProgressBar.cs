using System.Windows.Forms;

namespace ESDEUpdater;

public class ThemedProgressBar : ProgressBar
{
    private const int MarqueeTimerIntervalMs = 30;
    private const int MarqueeOffsetStep = 2;
    private const int MinMarqueeBandWidth = 20;

    private readonly System.Windows.Forms.Timer _marqueeTimer = new() { Interval = MarqueeTimerIntervalMs };
    private int _marqueeOffset;

    private Color _fillColorValue = Color.FromArgb(0, 120, 215);
    private Color _trackColorValue = Color.FromArgb(200, 200, 200);

    public Color FillColorValue
    {
        get => _fillColorValue;
        set
        {
            _fillColorValue = value;
            Invalidate();
        }
    }

    public Color TrackColorValue
    {
        get => _trackColorValue;
        set
        {
            _trackColorValue = value;
            Invalidate();
        }
    }

    public ThemedProgressBar()
    {
        SetStyle(ControlStyles.UserPaint, true);
        _marqueeTimer.Tick += (_, _) =>
        {
            if (Style == ProgressBarStyle.Marquee && Visible)
            {
                _marqueeOffset += MarqueeOffsetStep;
                Invalidate();
            }
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _marqueeTimer.Stop();
            _marqueeTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        _marqueeTimer.Enabled = Visible;
        if (Visible)
        {
            _marqueeOffset = 0;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        using (var trackBrush = new SolidBrush(TrackColorValue))
        {
            g.FillRectangle(trackBrush, ClientRectangle);
        }

        if (Style == ProgressBarStyle.Marquee)
        {
            DrawMarquee(g);
            return;
        }

        if (Maximum > Minimum)
        {
            var ratio = (Value - Minimum) / (double)(Maximum - Minimum);
            var fillWidth = (int)(ClientSize.Width * ratio);
            if (fillWidth > 0)
            {
                using var fillBrush = new SolidBrush(FillColorValue);
                g.FillRectangle(fillBrush, 0, 0, fillWidth, ClientSize.Height);
            }
        }
    }

    private void DrawMarquee(Graphics g)
    {
        var bandWidth = Math.Max(MinMarqueeBandWidth, ClientSize.Width / 4);
        var x = _marqueeOffset % (ClientSize.Width + bandWidth) - bandWidth;

        using var fillBrush = new SolidBrush(FillColorValue);
        g.FillRectangle(fillBrush, x, 0, bandWidth, ClientSize.Height);
    }
}
