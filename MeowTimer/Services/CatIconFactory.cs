using System.IO;
using System.Reflection;
using Draw = System.Drawing;
using Draw2D = System.Drawing.Drawing2D;

namespace MeowTimer.Services;

/// <summary>生成托盘图标：猫咪原图，或 CPU / 内存百分比。</summary>
public static class CatIconFactory
{
    private static byte[]? _icoBytes;

    private static byte[] IcoBytes()
    {
        if (_icoBytes != null) return _icoBytes;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream("MeowTimer.Assets.meow.ico");
            if (s == null) { _icoBytes = Array.Empty<byte>(); return _icoBytes; }
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            _icoBytes = ms.ToArray();
        }
        catch
        {
            _icoBytes = Array.Empty<byte>();
        }
        return _icoBytes;
    }

    public static Draw.Icon CatIcon(int size = 16)
    {
        var bytes = IcoBytes();
        if (bytes.Length == 0) return Draw.SystemIcons.Application;
        try
        {
            using var ms = new MemoryStream(bytes);
            using var baseIcon = new Draw.Icon(ms);
            using var sized = new Draw.Icon(baseIcon, size, size);
            return (Draw.Icon)sized.Clone();
        }
        catch
        {
            return Draw.SystemIcons.Application;
        }
    }

    public static Draw.Icon PercentIcon(int size, int value, Draw.Color bg, Draw.Color fg)
    {
        using var bmp = new Draw.Bitmap(size, size, Draw.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Draw.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Draw2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = Draw.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Draw.Color.Transparent);

            using (var brush = new Draw.SolidBrush(bg))
                g.FillEllipse(brush, 0, 0, size, size);

            string txt = Math.Clamp(value, 0, 100).ToString();
            float fs = size * (txt.Length >= 3 ? 0.44f : 0.62f);
            using var font = new Draw.Font("Segoe UI", fs, Draw.FontStyle.Bold, Draw.GraphicsUnit.Pixel);
            using var tb = new Draw.SolidBrush(fg);
            using var sf = new Draw.StringFormat
            {
                Alignment = Draw.StringAlignment.Center,
                LineAlignment = Draw.StringAlignment.Center
            };
            g.DrawString(txt, font, tb, new Draw.RectangleF(0, 0, size, size + size * 0.04f), sf);
        }

        IntPtr h = bmp.GetHicon();
        try
        {
            using var tmp = Draw.Icon.FromHandle(h);
            return (Draw.Icon)tmp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(h);
        }
    }
}
