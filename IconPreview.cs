using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace FF14DiscordRelay;

internal sealed class IconPreview : Control
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? Image { get; set; }

    public IconPreview()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Image is null)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

        var target = ClientRectangle;
        var imageRatio = (float)Image.Width / Image.Height;
        var boxRatio = (float)Math.Max(1, target.Width) / Math.Max(1, target.Height);
        if (imageRatio > boxRatio)
        {
            var height = (int)(target.Width / imageRatio);
            target.Y += (target.Height - height) / 2;
            target.Height = height;
        }
        else
        {
            var width = (int)(target.Height * imageRatio);
            target.X += (target.Width - width) / 2;
            target.Width = width;
        }

        e.Graphics.DrawImage(Image, target);
    }
}
