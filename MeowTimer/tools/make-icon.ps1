Add-Type -AssemblyName System.Drawing

$outDir = "E:\WorkBuddy\2026-11-10-10-44-32\MeowTimer\Assets"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function New-CatBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 64.0
    $g.ScaleTransform($s, $s)

    # ---- round background with pink gradient ----
    $rect = New-Object System.Drawing.RectangleF(2, 2, 60, 60)
    $c1 = [System.Drawing.Color]::FromArgb(255, 255, 143, 177)
    $c2 = [System.Drawing.Color]::FromArgb(255, 255, 183, 205)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45.0)
    $g.FillEllipse($brush, $rect)
    $brush.Dispose()

    # ---- ears (behind head) ----
    $earBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $earL = New-Object System.Drawing.Drawing2D.GraphicsPath
    $earL.AddPolygon(@(
        (New-Object System.Drawing.PointF(15, 25)),
        (New-Object System.Drawing.PointF(19, 9)),
        (New-Object System.Drawing.PointF(31, 19))
    ))
    $earR = New-Object System.Drawing.Drawing2D.GraphicsPath
    $earR.AddPolygon(@(
        (New-Object System.Drawing.PointF(49, 25)),
        (New-Object System.Drawing.PointF(45, 9)),
        (New-Object System.Drawing.PointF(33, 19))
    ))
    $g.FillPath($earBrush, $earL)
    $g.FillPath($earBrush, $earR)

    # inner ears
    $innerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 168, 195))
    $innerL = New-Object System.Drawing.Drawing2D.GraphicsPath
    $innerL.AddPolygon(@(
        (New-Object System.Drawing.PointF(18.5, 23)),
        (New-Object System.Drawing.PointF(20.5, 14)),
        (New-Object System.Drawing.PointF(27, 20))
    ))
    $innerR = New-Object System.Drawing.Drawing2D.GraphicsPath
    $innerR.AddPolygon(@(
        (New-Object System.Drawing.PointF(45.5, 23)),
        (New-Object System.Drawing.PointF(43.5, 14)),
        (New-Object System.Drawing.PointF(37, 20))
    ))
    $g.FillPath($innerBrush, $innerL)
    $g.FillPath($innerBrush, $innerR)

    # ---- head ----
    $g.FillEllipse($earBrush, (New-Object System.Drawing.RectangleF(11, 17, 42, 37)))

    # ---- eyes (closed happy arcs) ----
    $eyePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 92, 74, 86), 2.6)
    $eyePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $eyePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($eyePen, (New-Object System.Drawing.RectangleF(19, 28, 9, 8)), 200, 140)
    $g.DrawArc($eyePen, (New-Object System.Drawing.RectangleF(36, 28, 9, 8)), 200, 140)

    # ---- nose ----
    $noseBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 143, 177))
    $nose = New-Object System.Drawing.Drawing2D.GraphicsPath
    $nose.AddPolygon(@(
        (New-Object System.Drawing.PointF(29.5, 38.5)),
        (New-Object System.Drawing.PointF(34.5, 38.5)),
        (New-Object System.Drawing.PointF(32, 41.5))
    ))
    $g.FillPath($noseBrush, $nose)

    # ---- whiskers ----
    $whiskerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 255, 255, 255), 1.6)
    $whiskerPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $whiskerPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($whiskerPen, 12, 36, 20, 38)
    $g.DrawLine($whiskerPen, 12, 41, 20, 41)
    $g.DrawLine($whiskerPen, 52, 36, 44, 38)
    $g.DrawLine($whiskerPen, 52, 41, 44, 41)

    # ---- cheeks ----
    $cheekBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 255, 140, 175))
    $g.FillEllipse($cheekBrush, (New-Object System.Drawing.RectangleF(16, 40, 8, 5)))
    $g.FillEllipse($cheekBrush, (New-Object System.Drawing.RectangleF(40, 40, 8, 5)))

    $eyePen.Dispose(); $whiskerPen.Dispose(); $earBrush.Dispose(); $innerBrush.Dispose()
    $noseBrush.Dispose(); $cheekBrush.Dispose(); $earL.Dispose(); $earR.Dispose()
    $innerL.Dispose(); $innerR.Dispose(); $nose.Dispose()
    $g.Dispose()
    return $bmp
}

# build multi-size ico
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($sz in $sizes) {
    $bmp = New-CatBitmap $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,@($sz, $ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

$fs = [System.IO.File]::Create("$outDir\meow.ico")
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)                  # reserved
$bw.Write([UInt16]1)                  # type = icon
$bw.Write([UInt16]$sizes.Count)       # count

$offset = 6 + (16 * $sizes.Count)
foreach ($item in $pngs) {
    $sz = $item[0]; $data = $item[1]
    $dim = if ($sz -ge 256) { 0 } else { $sz }
    $bw.Write([Byte]$dim)             # width
    $bw.Write([Byte]$dim)             # height
    $bw.Write([Byte]0)                # palette
    $bw.Write([Byte]0)                # reserved
    $bw.Write([UInt16]1)              # planes
    $bw.Write([UInt16]32)             # bpp
    $bw.Write([UInt32]$data.Length)   # size in bytes
    $bw.Write([UInt32]$offset)        # offset
    $offset += $data.Length
}
foreach ($item in $pngs) { $bw.Write($item[1]) }
$bw.Flush(); $bw.Close(); $fs.Close()

# also save a 256 png for preview / window artwork
$preview = New-CatBitmap 256
$preview.Save("$outDir\meow_preview.png", [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Get-Item "$outDir\meow.ico" | Select-Object FullName, Length
